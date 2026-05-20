export interface EntitySummaryDto {
  Id: string
  Label: string
  EntityType: string
  Krs?: string
}

export interface NodeDataDto {
  Label: string
  EntityType: string
  FormaPrawna?: string
  IsActive: boolean
  WLikwidacji: boolean
  WUpadlosci: boolean
  IsCentrum?: boolean
  IsSubsidiary?: boolean
  BezProfilu?: boolean
}

export interface EdgeDataDto {
  RelationType: string
  RelationLabel?: string
  DataKoniec?: string | null
  IsActive: boolean
}

export interface GraphDto {
  Nodes: Array<{ Id: string; Type: string; Position: { X: number; Y: number }; Data: NodeDataDto }>
  Edges: Array<{ Id: string; Source: string; Target: string; Label?: string; Data: EdgeDataDto }>
}

export interface ConnectionSummaryDto {
  TargetId: string
  TargetLabel: string
  RelationType: string
  Opis?: string
  DataStart?: string
  IsActive: boolean
}

export interface EntityDetailDto {
  id: string
  label: string
  entityType: string
  krs?: string
  nip?: string
  Regon?: string
  FormaPrawna?: string
  PkdDzial?: string
  Adres?: string
  WLikwidacji?: boolean
  WUpadlosci?: boolean
  Connections: ConnectionSummaryDto[]
}
