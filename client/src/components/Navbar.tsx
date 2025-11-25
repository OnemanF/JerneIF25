import { Link, useLocation } from "react-router-dom";
import "./Footer.css";

export default function Navbar() {
    const logoSrc = "/logo_text.png";
    const { pathname } = useLocation();

    return (
        <header style={{
            display: "flex", alignItems: "center", justifyContent: "space-between",
            padding: "12px 24px", color: "#e9eef8"
        }}>
            <Link to="/" style={{ display: "inline-flex", alignItems: "center", gap: 8 }}>
                <img src={logoSrc} alt="Jerne IF – Dead Pigeons" style={{ height: 50, width: "auto", display: "block" }} />
            </Link>

            <div style={{ display: "inline-flex", alignItems: "center", gap: 12 }}>
                {pathname !== "/login" && (
                    <Link to="/login" className="primary">Log ind</Link>
                )}
            </div>
        </header>
    );
}
