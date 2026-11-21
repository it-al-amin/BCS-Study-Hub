export interface ApiResult<T> {
  data: T
  status: number
}

export interface PagedResult<T> {
  items: T[]
  pageNumber: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export class ApiError extends Error {
  readonly status: number

  constructor(
    message: string,
    status: number,
  ) {
    super(message)
    this.status = status
    this.name = 'ApiError'
  }
}

export class ApiClient {
  private readonly baseUrl = import.meta.env.VITE_API_BASE_URL ?? '/api/v1'

  protected async get<T>(path: string, token?: string): Promise<T> {
    return this.request<T>(path, { method: 'GET' }, token)
  }

  protected async post<T>(path: string, body: unknown, token?: string): Promise<T> {
    return this.request<T>(path, { method: 'POST', body: JSON.stringify(body) }, token)
  }

  protected async put<T>(path: string, body: unknown, token?: string): Promise<T> {
    return this.request<T>(path, { method: 'PUT', body: JSON.stringify(body) }, token)
  }

  protected async delete(path: string, token?: string): Promise<void> {
    await this.request<void>(path, { method: 'DELETE' }, token)
  }

  private async request<T>(path: string, init: RequestInit, token?: string): Promise<T> {
    const headers = new Headers(init.headers)
    headers.set('Accept', 'application/json')
    if (init.body !== undefined) headers.set('Content-Type', 'application/json')
    if (token) headers.set('Authorization', `Bearer ${token}`)

    const response = await fetch(`${this.baseUrl}${path}`, { ...init, headers })
    if (response.status === 204) return undefined as T
    if (!response.ok) {
      const body = await response.text()
      let detail = `API request failed (${response.status})`
      if (body) {
        try {
          const problem = JSON.parse(body) as { detail?: string; title?: string }
          detail = problem.detail ?? problem.title ?? detail
        } catch {
          detail = `API request failed (${response.status})`
        }
      }
      throw new ApiError(detail, response.status)
    }

    return response.json() as Promise<T>
  }
}
