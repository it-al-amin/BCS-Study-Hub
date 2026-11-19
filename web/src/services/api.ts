export interface ApiResult<T> {
  data: T
  status: number
}

export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export class ApiClient {
  private readonly baseUrl = import.meta.env.VITE_API_BASE_URL ?? '/api/v1'

  protected async get<T>(path: string): Promise<T> {
    const response = await fetch(`${this.baseUrl}${path}`, { headers: { Accept: 'application/json' } })
    if (!response.ok) throw new Error(`API request failed (${response.status})`)
    return response.json() as Promise<T>
  }
}
