import { FormEvent, useState } from "react";
import { useNavigate } from "react-router-dom";
import "./Login.css";

export default function Login() {
    const navigate = useNavigate();
    const [mode, setMode] = useState<"login" | "register">("login");
    const [name, setName] = useState("");
    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");

    function onSubmit(e: FormEvent) {
        e.preventDefault();
        navigate("/Play"); 
    }

    return (
        <div className="auth-wrap">
            <div className="auth-card">
                <h1 className="auth-title">{mode === "login" ? "Log ind" : "Opret spiller"}</h1>
                <p className="auth-subtitle">
                    {mode === "login" ? "Brug din e-mail og adgangskode. Rollen vælges automatisk."
                        : "Udfyld for at oprette en ny spiller."}
                </p>

                <form className="auth-form auth-form--flush" onSubmit={onSubmit}>
                    {mode === "register" && (
                        <label className="field">
                            Navn
                            <input className="input" type="text" value={name} onChange={(e) => setName(e.target.value)} placeholder="Dit navn" required />
                        </label>
                    )}
                    <label className="field">
                        E-mail
                        <input className="input" type="email" value={email} onChange={(e) => setEmail(e.target.value)} placeholder="mads@example.com" required />
                    </label>
                    <label className="field">
                        Adgangskode
                        <input className="input" type="password" value={password} onChange={(e) => setPassword(e.target.value)} placeholder="••••••••" required minLength={6} />
                    </label>

                    <button className="btn btn--primary btn--full">
                        {mode === "login" ? "Log ind" : "Opret spiller"}
                    </button>
                </form>

                <div className="auth-divider" />
                {mode === "login" ? (
                    <button className="btn btn--ghost btn--full" onClick={() => setMode("register")}>
                        Opret ny spiller
                    </button>
                ) : (
                    <button className="btn btn--ghost btn--full" onClick={() => setMode("login")}>
                        Har du allerede en konto? Log ind
                    </button>
                )}
            </div>
        </div>
    );
}
