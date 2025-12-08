import { Link, useLocation, useNavigate } from "react-router-dom";
import "./Footer.css";
import { useAuth } from "../auth/AuthContext";

export default function Navbar() {
    const logoSrc = "/logo_text.png";
    const { pathname } = useLocation();
    const navigate = useNavigate();
    const { user, signOut } = useAuth();

    const loggedIn = !!user;

    function onAuthClick() {
        if (loggedIn) {
            signOut();
            navigate("/login", { replace: true });
        } else {
            navigate("/login");
        }
    }

    return (
        <header
            style={{
                display: "flex",
                alignItems: "center",
                justifyContent: "space-between",
                padding: "12px 24px",
                color: "#e9eef8",
            }}
        >
            <Link to="/" style={{ display: "inline-flex", alignItems: "center", gap: 8 }}>
                <img
                    src={logoSrc}
                    alt="Jerne IF – Dead Pigeons"
                    style={{ height: 50, width: "auto", display: "block" }}
                />
            </Link>

            <div style={{ display: "inline-flex", alignItems: "center", gap: 12 }}>
                {}
                {pathname !== "/login" && (
                    <button
                        className={loggedIn ? "balance-btn" : "primary"}
                        onClick={onAuthClick}
                    >
                        {loggedIn ? "Log ud" : "Log ind"}
                    </button>
                )}
            </div>
        </header>
    );
}
