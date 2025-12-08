import { FormEvent, useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import './Login.css';
import { useAuth } from '../auth/AuthContext';

export default function Login() {
    const navigate = useNavigate();
    const auth = useAuth();
    const [mode, setMode] = useState<'login' | 'register'>('login');
    const [name, setName] = useState('');
    const [phone, setPhone] = useState('');
    const [email, setEmail] = useState('');
    const [password, setPassword] = useState('');
    const [err, setErr] = useState<string | null>(null);

    useEffect(() => {
        if (!auth.hydrated) return;
        if (auth.user) {
            navigate(auth.user.role === 'admin' ? '/admin' : '/Play', { replace: true });
        }
    }, [auth.hydrated, auth.user, navigate]);

    useEffect(() => {
        document.body.classList.add('login-no-scroll');
        return () => document.body.classList.remove('login-no-scroll');
    }, []);

    async function onSubmit(e: FormEvent) {
        e.preventDefault();
        setErr(null);
        try {
            if (mode === 'login') {
                const role = await auth.signIn(email, password);
                navigate(role === 'admin' ? '/admin' : '/Play', { replace: true });
            } else {
                await auth.registerPlayer({ name, email, password, phone });
                navigate('/Play', { replace: true });
            }
        } catch (ex: any) {
            setErr(ex?.message ?? 'Noget gik galt.');
        }
    }

    if (!auth.hydrated) {
        return (
            <div className="auth-wrap">
                <div className="auth-card"><p className="auth-subtitle">Henter session…</p></div>
            </div>
        );
    }
    if (auth.user) return null;

    return (
        <div className="auth-wrap">
            <div className="auth-card">
                <h1 className="auth-title">Log ind</h1>
                <p className="auth-subtitle">
                    Log ind eller opret en konto for at få adgang til alt indhold fra Døde Duer.
                </p>

                {err && <div className="chip chip--muted" style={{ marginBottom: 8 }}>{err}</div>}

                <form className="auth-form" onSubmit={onSubmit}>
                    {mode === 'register' && (
                        <>
                            <label className="field">Navn
                                <input className="input" type="text" value={name} onChange={e => setName(e.target.value)} required />
                            </label>
                            <label className="field">Telefon (valgfri)
                                <input className="input" type="tel" value={phone} onChange={e => setPhone(e.target.value)} />
                            </label>
                        </>
                    )}

                    <label className="field">E-mail
                        <input className="input" type="email" value={email} onChange={e => setEmail(e.target.value)} required />
                    </label>

                    <label className="field">Adgangskode
                        <input className="input" type="password" value={password} onChange={e => setPassword(e.target.value)} required minLength={6} />
                    </label>

                    {}
                    <div className="row row--right">
                        <button
                            type="button"
                            className="link"
                            onClick={() => alert('Kontakt en admin for nulstilling.')}
                        >
                            Glemt adgangskode?
                        </button>
                    </div>

                    {}
                    <button className="btn btn--primary btn--mid" disabled={auth.loading}>
                        {mode === 'login'
                            ? (auth.loading ? 'Logger ind…' : 'Log ind')
                            : (auth.loading ? 'Opretter…' : 'Opret spiller')}
                    </button>
                </form>

                {}
                <div className="below">
                    {mode === 'login' ? (
                        <button className="btn btn--ghost btn--small" onClick={() => setMode('register')}>
                            Opret ny spiller
                        </button>
                    ) : (
                        <button className="btn btn--ghost btn--small" onClick={() => setMode('login')}>
                            Har du allerede en konto? Log ind
                        </button>
                    )}
                </div>
            </div>
        </div>
    );
}
