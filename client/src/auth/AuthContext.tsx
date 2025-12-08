import { createContext, useContext, useRef, useState, useEffect, useMemo } from 'react'
import { http, setToken, clearToken, getToken } from '@lib/http'

type User = { email: string; role: 'admin' | 'player' }

type AuthCtx = {
    user: User | null
    loading: boolean
    hydrated: boolean
    signIn: (email: string, password: string) => Promise<'admin' | 'player'>
    registerPlayer: (args: { name: string; email: string; password: string; phone?: string }) => Promise<void>
    signOut: () => void
}

const AuthContext = createContext<AuthCtx>({
    user: null,
    loading: false,
    hydrated: false,
    signIn: async () => 'player',
    registerPlayer: async () => {},
    signOut: () => {},
})

export function AuthProvider({ children }: { children: React.ReactNode }) {
    const [user, setUser] = useState<User | null>(null)
    const [loading, setLoading] = useState(false)
    const [hydrated, setHydrated] = useState(false) 
    
    const booted = useRef(false)
    useEffect(() => {
        if (booted.current) return
        booted.current = true

        const token = getToken()
        if (!token) {
            setHydrated(true)
            return
        }
        
        fetchWhoAmI()
            .then(setUser)
            .catch((err: any) => {
                const status = err?.status ?? err?.response?.status
                if (status === 401 || status === 403) clearToken()
            })
            .finally(() => setHydrated(true))
    }, [])

    async function fetchWhoAmI(): Promise<User> {
        const me = await http.get<{ email: string; roles: string[]; sub: string }>('/auth/whoami')
        const role = (me.roles?.includes('admin') ? 'admin' : 'player') as 'admin' | 'player'
        return { email: me.email, role }
    }

    async function signIn(email: string, password: string): Promise<'admin' | 'player'> {
        setLoading(true)
        try {
            let res: { token: string }
            try {
                res = await http.post('/auth/admin/login', { email, password })
            } catch {
                res = await http.post('/auth/player/login', { email, password })
            }
            setToken(res.token)

            const u = await fetchWhoAmI()
            setUser(u)
            return u.role
        } finally {
            setLoading(false)
            setHydrated(true)
        }
    }

    async function registerPlayer(args: { name: string; email: string; password: string; phone?: string }) {
        setLoading(true)
        try {
            const res = await http.post<{ token: string }>('/auth/player/register', args)
            setToken(res.token)
            const u = await fetchWhoAmI()
            setUser(u)
        } finally {
            setLoading(false)
            setHydrated(true)
        }
    }

    function signOut() {
        clearToken()
        setUser(null)
    }
    
    const value = useMemo(
        () => ({ user, loading, hydrated, signIn, registerPlayer, signOut }),
        [user, loading, hydrated]
    )

    return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export const useAuth = () => useContext(AuthContext)
