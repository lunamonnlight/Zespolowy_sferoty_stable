import { ref } from 'vue'
import { MarkerType } from '@vue-flow/core'
import type { Node, Edge } from '@vue-flow/core'
import * as d3 from 'd3'
import type { EntitySummaryDto, NodeDataDto, GraphDto, EntityDetailDto } from '../types/graph'

const BASE_URL = 'http://localhost:5100/api/BusinessConnections'

interface SimNode extends d3.SimulationNodeDatum {
  id: string
  data: any
}

export function useGraph() {
  const query         = ref('')
  const loading       = ref(false)
  const errorMsg      = ref('')
  const searchResults = ref<EntitySummaryDto[]>([])
  const graphLoaded   = ref(false)
  const nodes         = ref<Node[]>([])
  const edges         = ref<Edge[]>([])
  const expandedIds   = ref<Set<string>>(new Set())
  const expandingId   = ref<string | null>(null)
  const contextMenu   = ref<{ x: number; y: number; node: Node } | null>(null)
  const contextDetail   = ref<EntityDetailDto | null>(null)
  const contextLoading  = ref(false)

  // ── D3 force layout ──────────────────────────────────────────────────────────

  function runSimulation(
      simNodes: SimNode[],
      simLinks: { source: string; target: string }[],
      centerX = 500,
      centerY = 400,
      rootId?: string,
  ): Map<string, { x: number; y: number }> {
    const nodeCount = simNodes.length

    const linkDistance  = Math.max(160, 120 + nodeCount * 4)
    const chargeStr     = Math.min(-800, -600 - nodeCount * 30)
    const collideRadius = (d: any) => (d.data?.entityType ?? d.data?.EntityType) === 'osoba' ? 80 : 100
    const radialInner   = Math.max(250, 180 + nodeCount * 5)

    const simulation = d3.forceSimulation<SimNode>(simNodes)
        .force('link', d3.forceLink(simLinks)
            .id((d: any) => d.id)
            .distance((link: any) => {
              const sourceDeg = simLinks.filter(l => l.source === link.source.id || l.target === link.source.id).length
              const targetDeg = simLinks.filter(l => l.source === link.target.id || l.target === link.target.id).length
              const minDeg = Math.min(sourceDeg, targetDeg)
              if (minDeg <= 1) return linkDistance * 0.5
              if (minDeg <= 3) return linkDistance * 0.75
              return linkDistance
            })
            .strength(0.8))
        .force('charge', d3.forceManyBody().strength(chargeStr).distanceMax(3000))
        .force('collision', d3.forceCollide().radius(collideRadius).strength(1.5).iterations(4))
        .force('center', d3.forceCenter(centerX, centerY).strength(0.02))
        .force('radial', d3.forceRadial((d: any) => {
          if (d.id === rootId) return 0
          const degree = simLinks.filter(l => l.source === d.id || l.target === d.id).length
          if (degree >= 5) return radialInner
          if (degree >= 2) return radialInner * 0.7
          return radialInner * 0.5
        }, centerX, centerY).strength(0.8))
        .stop()

    const ticks = Math.max(400, 300 + nodeCount * 10)
    simulation.tick(ticks)

    const positions = new Map<string, { x: number; y: number }>()
    simNodes.forEach(n => positions.set(n.id, { x: n.x ?? centerX, y: n.y ?? centerY }))
    return positions
  }

  // ── Helpers ──────────────────────────────────────────────────────────────────

  function getNodeType(data: any): string {
    const isCentrum   = data?.isCentrum   ?? data?.IsCentrum
    const entityType  = data?.entityType  ?? data?.EntityType
    const bezProfilu  = data?.bezProfilu  ?? data?.BezProfilu
    if (isCentrum)                                              return 'organization-centrum'
    if (entityType === 'osoba-bez-pesel' || bezProfilu)        return 'person-foreign'
    if (entityType === 'osoba')                                 return 'person'
    return 'organization'
  }

  function nodeId(n: any): string   { return n.id ?? n.Id }
  function nodeData(n: any): any    { return n.data ?? n.Data }
  function edgeSrc(e: any): string  { return e.source ?? e.Source }
  function edgeTgt(e: any): string  { return e.target ?? e.Target }
  function edgeId(e: any): string   { return e.id ?? e.Id }

  function buildEdge(edgeDtos: any[]): Edge[] {
    const groups = new Map<string, any[]>()
    for (const e of edgeDtos) {
      const key = [edgeSrc(e), edgeTgt(e)].sort().join('__')
      if (!groups.has(key)) groups.set(key, [])
      groups.get(key)!.push(e)
    }

    const result: Edge[] = []
    for (const group of groups.values()) {
      const isActive = group.some(e => {
        const d = nodeData(e)
        return d?.isActive ?? d?.IsActive
      })
      const color  = isActive ? '#4ade80' : '#6b7280'
      const labels = [...new Set(
          group.map(e => {
            const d = nodeData(e)
            return e.label ?? e.Label ?? d?.relationLabel ?? d?.RelationLabel ?? d?.relationType ?? d?.RelationType ?? ''
          }).map((l: string) => l.trim()).filter(Boolean)
      )]
      const rep = group[0]
      result.push({
        id:     edgeId(rep),
        source: edgeSrc(rep),
        target: edgeTgt(rep),
        type: 'straight',
        label: labels.join(' / ') || undefined,
        animated: isActive,
        style: { stroke: color, strokeWidth: 1.5 },
        labelStyle: { fill: '#1e293b', fontSize: 11 },
        labelBgStyle: { fill: 'var(--surface-800)', fillOpacity: 0.85 },
        markerEnd: { type: MarkerType.ArrowClosed, color },
      })
    }
    return result
  }

  // ── Wyszukiwanie ─────────────────────────────────────────────────────────────

  let debounceTimer: ReturnType<typeof setTimeout>

  function onInput() {
    clearTimeout(debounceTimer)
    errorMsg.value = ''
    if (query.value.trim().length < 2) { searchResults.value = []; return }
    debounceTimer = setTimeout(handleSearch, 400)
  }

  async function handleSearch() {
    if (!query.value.trim()) return
    errorMsg.value = ''
    loading.value = true
    searchResults.value = []
    try {
      const res = await fetch(`${BASE_URL}/search?query=${encodeURIComponent(query.value)}`)
      if (!res.ok) throw new Error()
      searchResults.value = await res.json()
      if (!searchResults.value.length) errorMsg.value = 'Brak wyników dla podanej frazy.'
    } catch {
      errorMsg.value = 'Błąd połączenia z backendem.'
    } finally {
      loading.value = false
    }
  }

  async function onResultSelect(result: any) {
    searchResults.value = []
    query.value = result.label
    await loadFullGraph(result.id)
  }

  async function loadFullGraph(entityId: string) {
    searchResults.value = []
    loading.value = true
    errorMsg.value = ''
    graphLoaded.value = false
    expandedIds.value = new Set()
    expandingId.value = null
    nodes.value = []
    edges.value = []

    try {
      const res = await fetch(`${BASE_URL}/graph?entityId=${encodeURIComponent(entityId)}`)
      if (!res.ok) throw new Error()
      const graph = await res.json()
      applyGraph(graph)
      graphLoaded.value = true
    } catch (err) {
      console.error('loadFullGraph error:', err)
      errorMsg.value = 'Nie udało się pobrać grafu powiązań.'
    } finally {
      loading.value = false
    }
  }

  // ── applyGraph ────────────────────────────────────────────────────────────────

  function applyGraph(graph: any) {
    const graphNodes = graph.nodes ?? graph.Nodes ?? []
    const graphEdges = graph.edges ?? graph.Edges ?? []

    const centrumNode = graphNodes.find((n: any) => {
      const d = nodeData(n)
      return d?.isCentrum ?? d?.IsCentrum
    })
    const centrumId = nodeId(centrumNode ?? graphNodes[0] ?? {})

    const simNodes: SimNode[] = graphNodes.map((n: any) => ({
      id:   nodeId(n),
      data: nodeData(n),
      ...(nodeId(n) === centrumId ? { fx: 500, fy: 400 } : {}),
    }))

    const simLinks = graphEdges.map((e: any) => ({ source: edgeSrc(e), target: edgeTgt(e) }))
    const positions = runSimulation(simNodes, simLinks, 500, 400, centrumId)

    nodes.value = graphNodes.map((n: any) => ({
      id:       nodeId(n),
      type:     getNodeType(nodeData(n)),
      position: positions.get(nodeId(n))!,
      data:     nodeData(n),
    }))

    edges.value = buildEdge(graphEdges)
  }

  // ── Kliknięcie w węzeł ────────────────────────────────────────────────────────

  async function onNodeClick({ node }: { node: Node }) {
    const nId = node.id
    if (nId === 'org-centrum') return
    if (expandedIds.value.has(nId) || expandingId.value !== null) return

    expandingId.value = nId
    errorMsg.value = ''

    try {
      const knownNodeIds = nodes.value.map(n => n.id)
      const res = await fetch(
          `${BASE_URL}/expand/${encodeURIComponent(nId)}`,
          {
            method:  'POST',
            headers: { 'Content-Type': 'application/json' },
            body:    JSON.stringify(knownNodeIds),
          }
      )

      if (!res.ok) throw new Error(`HTTP ${res.status}`)
      const graph = await res.json()

      const graphNodes = graph.nodes ?? graph.Nodes ?? []
      const graphEdges = graph.edges ?? graph.Edges ?? []

      if (graphNodes.length === 0 && graphEdges.length === 0) {
        expandedIds.value = new Set([...expandedIds.value, nId])
        return
      }

      mergeGraph(graphNodes, graphEdges, node)
      expandedIds.value = new Set([...expandedIds.value, nId])
    } catch (err) {
      console.error('onNodeClick error:', err)
      errorMsg.value = 'Nie udało się pobrać powiązań węzła.'
    } finally {
      expandingId.value = null
    }
  }

  // ── mergeGraph ────────────────────────────────────────────────────────────────

  function mergeGraph(newRawNodes: any[], newRawEdges: any[], clickedNode: Node) {
    const existingNodeIds   = new Set(nodes.value.map(n => n.id))
    const existingEdgePairs = new Set(edges.value.map(e => [e.source, e.target].sort().join('__')))

    const filteredNodes = newRawNodes.filter(n => !existingNodeIds.has(nodeId(n)))
    const filteredEdges = newRawEdges.filter(e => {
      const key = [edgeSrc(e), edgeTgt(e)].sort().join('__')
      return !existingEdgePairs.has(key)
    })

    if (!filteredNodes.length && !filteredEdges.length) return

    const allSimNodes: SimNode[] = [
      ...nodes.value.map(n => ({ id: n.id, data: n.data, x: n.position.x, y: n.position.y })),
      ...filteredNodes.map(n => ({
        id:   nodeId(n),
        data: nodeData(n),
        x:    clickedNode.position.x + (Math.random() - 0.5) * 60,
        y:    clickedNode.position.y + (Math.random() - 0.5) * 60,
      })),
    ]

    const allEdgeLinks = [
      ...edges.value.map(e => ({ source: e.source, target: e.target })),
      ...filteredEdges.map(e => ({ source: edgeSrc(e), target: edgeTgt(e) })),
    ]

    const allNodeIds = new Set(allSimNodes.map(n => n.id))
    const simLinks = allEdgeLinks.filter(e => allNodeIds.has(e.source) && allNodeIds.has(e.target))

    const centrumNode = nodes.value.find(n => (n.data as any)?.isCentrum ?? (n.data as any)?.IsCentrum)
    const positions = runSimulation(
        allSimNodes, simLinks,
        centrumNode?.position.x ?? clickedNode.position.x,
        centrumNode?.position.y ?? clickedNode.position.y,
        centrumNode?.id ?? clickedNode.id,
    )

    nodes.value = nodes.value.map(n => ({ ...n, position: positions.get(n.id) ?? n.position }))

    const addedNodes: Node[] = filteredNodes.map(n => ({
      id:       nodeId(n),
      type:     getNodeType(nodeData(n)),
      position: positions.get(nodeId(n)) ?? { x: clickedNode.position.x + 250, y: clickedNode.position.y },
      data:     nodeData(n),
    }))

    nodes.value = [...nodes.value, ...addedNodes]
    edges.value = [...edges.value, ...buildEdge(filteredEdges)]
  }

  // ── Menu kontekstowe ──────────────────────────────────────────────────────────

  async function onNodeContextMenu({ event, node }: { event: MouseEvent; node: Node }) {
    event.preventDefault()
    contextMenu.value = { x: event.clientX, y: event.clientY, node }
    contextDetail.value = null
    contextLoading.value = true
    try {
      const res = await fetch(`${BASE_URL}/entity/${encodeURIComponent(node.id)}`)
      if (res.ok) contextDetail.value = await res.json()
    } catch {
      // ignorujemy błąd
    } finally {
      contextLoading.value = false
    }
  }

  function closeContextMenu() {
    contextMenu.value = null
    contextDetail.value = null
  }

  // ── Minimap kolor ─────────────────────────────────────────────────────────────

  function minimapColor(node: Node): string {
    if (node.type === 'organization-centrum') return '#f59e0b'
    if (node.type === 'person' || node.type === 'person-foreign') return '#2dd4bf'
    return '#818cf8'
  }

  return {
    query, loading, errorMsg, searchResults, graphLoaded,
    nodes, edges, expandedIds, expandingId,
    contextMenu, contextDetail, contextLoading,
    onInput, handleSearch, onResultSelect,
    onNodeClick, onNodeContextMenu, closeContextMenu,
    minimapColor,
  }
}
