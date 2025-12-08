import { API_BASE } from '@lib/config';
if (typeof window !== 'undefined') {
    console.log('[API_BASE]', API_BASE);
}

export function getToken(): string { return localStorage.getItem('token') ?? ''; }
export function setToken(token: string) { localStorage.setItem('token', token); }
export function clearToken() { localStorage.removeItem('token'); }

type HttpMethod = 'GET'|'POST'|'PUT'|'DELETE';

function buildHeaders(hasBody: boolean): HeadersInit {
    const h: Record<string,string> = { Accept: 'application/json' };
    if (hasBody) h['Content-Type'] = 'application/json';
    const token = getToken();
    if (token) h['Authorization'] = `Bearer ${token}`;
    return h;
}

async function request<T>(method: HttpMethod, path: string, body?: unknown): Promise<T> {
    const res = await fetch(`${API_BASE}${path}`, {
        method,
        headers: buildHeaders(body !== undefined),
        body: body !== undefined ? JSON.stringify(body) : undefined,
    });

    if (res.status === 204) return undefined as T;
    if (!res.ok) {
        const txt = await res.text().catch(()=> '');
        throw new Error(`${res.status} ${res.statusText}${txt ? `: ${txt}` : ''}`);
    }
    const ct = res.headers.get('content-type') || '';
    if (!ct.includes('application/json')) return undefined as T;
    return res.json() as Promise<T>;
}

export const http = {
    get:  <T>(p: string) => request<T>('GET', p),
    post: <T>(p: string, b?: unknown) => request<T>('POST', p, b),
    put:  <T>(p: string, b?: unknown) => request<T>('PUT', p, b),
    del:  <T>(p: string) => request<T>('DELETE', p),
};
