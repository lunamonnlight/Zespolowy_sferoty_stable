const STORAGE_KEY = 'filter_templates'

export const getTemplates = (): FilterTemplate[] => {
    const data = localStorage.getItem(STORAGE_KEY)
    return data ? JSON.parse(data) : []
}

export const saveTemplates = (templates: FilterTemplate[]) => {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(templates))
}

export const addTemplate = (template: FilterTemplate) => {
    const templates = getTemplates()
    templates.push(template)
    saveTemplates(templates)
}

export const deleteTemplate = (id: string) => {
    const templates = getTemplates().filter(t => t.id !== id)
    saveTemplates(templates)
}