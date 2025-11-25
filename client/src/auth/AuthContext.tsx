import { createContext, useContext } from "react";

type AuthCtx = {
    user: null;                  
    loading: false;
    signIn: (email: string, password: string) => Promise<void>;
    registerPlayer: (args: { name: string; email: string; password: string }) => Promise<void>;
    signOut: () => void;
};

const noop = async () => {};
const AuthContext = createContext<AuthCtx>({
    user: null,
    loading: false,
    signIn: noop,
    registerPlayer: noop,
    signOut: () => {},
});

export function AuthProvider({ children }: { children: React.ReactNode }) {
    return <AuthContext.Provider value={{
        user: null, loading: false, signIn: noop, registerPlayer: noop, signOut: () => {}
    }}>{children}</AuthContext.Provider>;
}

export const useAuth = () => useContext(AuthContext);