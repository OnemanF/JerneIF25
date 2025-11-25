import { useMemo, useState, useEffect } from "react";
import "./AdminDashboard.css";
import "../Footer.css";
import ThisWeekWinners from "./ThisWeekWinners";

type Player = { id: number; name: string; email: string; phone?: string; active: boolean };
type Game = { id: number; week: number; year: number; boards: number; revenueDkk: number; winning?: number[] };

const seedPlayers: Player[] = [
    { id: 1, name: "Mads Mikkelsen", email: "mads@example.com", phone: "12345678", active: true },
    { id: 2, name: "Sofie Larsen", email: "sofie@example.com", phone: "22334455", active: true },
    { id: 3, name: "Jonas Poulsen", email: "jonas@example.com", phone: "66778899", active: false },
];

const seedGames: Game[] = [
    { id: 102, week: 47, year: 2025, boards: 14, revenueDkk: 820, winning: [6, 7, 12] },
    { id: 101, week: 48, year: 2025, boards: 12, revenueDkk: 640, winning: [3, 11, 15] },
];

const WIN_COUNT = 3;

function getISOWeek(d: Date) {
    const date = new Date(Date.UTC(d.getFullYear(), d.getMonth(), d.getDate()));
    const dayNum = date.getUTCDay() || 7;
    date.setUTCDate(date.getUTCDate() + 4 - dayNum);
    const yearStart = new Date(Date.UTC(date.getUTCFullYear(), 0, 1));
    return Math.ceil((((date as any) - (yearStart as any)) / 86400000 + 1) / 7);
}
function weeksInISOYear(year: number) {
    return getISOWeek(new Date(Date.UTC(year, 11, 28))); // ISO trick
}
function nextWeekYear(week: number, year: number) {
    const max = weeksInISOYear(year);
    return week < max ? { week: week + 1, year } : { week: 1, year: year + 1 };
}

export default function AdminDashboard() {
    const [tab, setTab] = useState<"history" | "players" | "winners" | "setWinners">("history");
    const [players, setPlayers] = useState<Player[]>(seedPlayers);
    const [games, setGames] = useState<Game[]>(seedGames);
    const [query, setQuery] = useState("");
    const [sortDesc, setSortDesc] = useState(true);
    
    const now = new Date();
    const [currentWeek, setCurrentWeek] = useState(getISOWeek(now));
    const [currentYear, setCurrentYear] = useState(now.getFullYear());
    
    useEffect(() => {
        setGames((gs) => {
            const exists = gs.some((g) => g.week === currentWeek && g.year === currentYear);
            if (exists) return gs;
            const id = Math.max(0, ...gs.map((g) => g.id)) + 1;
            const placeholder: Game = { id, week: currentWeek, year: currentYear, boards: 0, revenueDkk: 0, winning: [] };
            return [placeholder, ...gs];
        });
    }, [currentWeek, currentYear]);

    const thisWeekGame = useMemo(
        () => games.find((g) => g.week === currentWeek && g.year === currentYear),
        [games, currentWeek, currentYear]
    );
    
    const [showModal, setShowModal] = useState(false);
    const [editing, setEditing] = useState<Player | null>(null);
    const [form, setForm] = useState<{ name: string; email: string; phone: string; active: boolean }>({
        name: "", email: "", phone: "", active: true,
    });

    function openNew() {
        setEditing(null);
        setForm({ name: "", email: "", phone: "", active: true });
        setShowModal(true);
    }
    function openEdit(p: Player) {
        setEditing(p);
        setForm({ name: p.name, email: p.email, phone: p.phone || "", active: p.active });
        setShowModal(true);
    }
    function savePlayer() {
        if (!form.name.trim() || !form.email.trim()) return;
        if (editing) {
            setPlayers((ps) =>
                ps.map((p) =>
                    p.id === editing.id
                        ? { ...p, name: form.name.trim(), email: form.email.trim(), phone: form.phone.trim(), active: form.active }
                        : p
                )
            );
        } else {
            const id = Math.max(0, ...players.map((p) => p.id)) + 1;
            setPlayers((ps) => [...ps, { id, name: form.name.trim(), email: form.email.trim(), phone: form.phone.trim(), active: form.active }]);
        }
        setShowModal(false);
    }
    function toggleActive(id: number) {
        setPlayers((ps) => ps.map((p) => (p.id === id ? { ...p, active: !p.active } : p)));
    }
    function deletePlayer(id: number) {
        if (!window.confirm("Slet spiller? Dette kan ikke fortrydes.")) return;
        setPlayers((ps) => ps.filter((p) => p.id !== id));
        if (editing?.id === id) setShowModal(false);
    }

    const filteredPlayers = useMemo(() => {
        const q = query.trim().toLowerCase();
        if (!q) return players;
        return players.filter(
            (p) =>
                p.name.toLowerCase().includes(q) ||
                p.email.toLowerCase().includes(q) ||
                (p.phone || "").toLowerCase().includes(q)
        );
    }, [players, query]);

    const sortedGames = useMemo(() => {
        const arr = [...games].sort((a, b) => (sortDesc ? (b.year*100 + b.week) - (a.year*100 + a.week)
            : (a.year*100 + a.week) - (b.year*100 + b.week)));
        return arr;
    }, [games, sortDesc]);
    
    const [picks, setPicks] = useState<number[]>([]);
    useEffect(() => {
        setPicks((thisWeekGame?.winning ?? []).slice(0, WIN_COUNT));
    }, [thisWeekGame?.id]);

    function togglePick(n: number) {
        setPicks((prev) => prev.includes(n) ? prev.filter((x) => x !== n)
            : prev.length >= WIN_COUNT ? prev
                : [...prev, n].sort((a, b) => a - b));
    }

    function saveWinners() {
        if (picks.length !== WIN_COUNT) return;
        
        setGames((gs) =>
            gs.map((g) => (g.week === currentWeek && g.year === currentYear ? { ...g, winning: [...picks] } : g))
        );
        
        const next = nextWeekYear(currentWeek, currentYear);
        setGames((gs) => {
            const exists = gs.some((g) => g.week === next.week && g.year === next.year);
            if (exists) return gs;
            const id = Math.max(0, ...gs.map((g) => g.id)) + 1;
            const placeholder: Game = { id, week: next.week, year: next.year, boards: 0, revenueDkk: 0, winning: [] };
            return [placeholder, ...gs];
        });
        
        setCurrentWeek(next.week);
        setCurrentYear(next.year);
        setPicks([]);
        setTab("winners");
    }

    function resetPicks() {
        setPicks((thisWeekGame?.winning ?? []).slice(0, WIN_COUNT));
    }

    return (
        <div className="page admin-page" style={{ paddingBottom: 40 }}>
            <header className="admin-hero">
                <div className="admin-hero__title">Admin</div>
                <div className="admin-hero__meta">Dashboard</div>
            </header>

            <div className="admin-card">
                <div className="admin-tabs" role="tablist" aria-label="Admin faner">
                    <button className={`admin-tab ${tab === "history" ? "is-active" : ""}`} onClick={() => setTab("history")}>Historik</button>
                    <button className={`admin-tab ${tab === "players" ? "is-active" : ""}`} onClick={() => setTab("players")}>Spillere</button>
                    <button className={`admin-tab ${tab === "winners" ? "is-active" : ""}`} onClick={() => setTab("winners")}>Vindertal</button>
                    <button className={`admin-tab ${tab === "setWinners" ? "is-active" : ""}`} onClick={() => setTab("setWinners")}>Sæt vindertal</button>
                </div>

                {tab === "history" && (
                    <section>
                        <div className="admin-row">
                            <div className="admin-row__left"><span className="admin-muted">Tidligere spil</span></div>
                            <div className="admin-row__right">
                                <button className="balance-btn" onClick={() => setSortDesc((s) => !s)}>
                                    Sortér: {sortDesc ? "Nyeste først" : "Ældste først"}
                                </button>
                            </div>
                        </div>
                        <div className="admin-table">
                            <div className="admin-thead"><div>Uge</div><div>År</div><div>Kuponer</div><div>Omsætning</div><div>Vindertal</div></div>
                            {sortedGames.map((g) => (
                                <div className="admin-trow" key={g.id}>
                                    <div>{g.week}</div><div>{g.year}</div><div>{g.boards}</div><div>{g.revenueDkk} DKK</div>
                                    <div className="wins">{(g.winning || []).map((n) => <span key={n} className="win-ball">{n}</span>)}</div>
                                </div>
                            ))}
                        </div>
                    </section>
                )}

                {tab === "players" && (
                    <section>
                        <div className="admin-row">
                            <div className="admin-row__left">
                                <input className="admin-input" placeholder="Søg spiller (navn, e-mail, tlf.)" value={query} onChange={(e) => setQuery(e.target.value)} />
                            </div>
                            <div className="admin-row__right"><button className="primary" onClick={openNew}>Ny spiller</button></div>
                        </div>
                        <div className="admin-table">
                            <div className="admin-thead"><div>Navn</div><div>E-mail</div><div>Telefon</div><div>Status</div><div>Handling</div></div>
                            {filteredPlayers.map((p) => (
                                <div className="admin-trow" key={p.id}>
                                    <div>{p.name}</div>
                                    <div className="admin-mono">{p.email}</div>
                                    <div>{p.phone || "-"}</div>
                                    <div><span className={`chip ${p.active ? "chip--ok" : "chip--muted"}`}>{p.active ? "Aktiv" : "Inaktiv"}</span></div>
                                    <div className="admin-actions">
                                        <button className="balance-btn" onClick={() => openEdit(p)}>Rediger</button>
                                        <button className="balance-btn" onClick={() => toggleActive(p.id)}>{p.active ? "Deaktivér" : "Aktivér"}</button>
                                        <button className="danger-btn" onClick={() => deletePlayer(p.id)}>Slet</button>
                                    </div>
                                </div>
                            ))}
                        </div>
                    </section>
                )}

                {tab === "winners" && (
                    <section>
                        <ThisWeekWinners games={games} week={currentWeek} year={currentYear} />
                        <p className="admin-muted">Tidligere vindertal (seneste først)</p>
                        <div className="wins-grid">
                            {sortedGames.map((g) => (
                                <div key={g.id} className="wins-card">
                                    <div className="wins-head">Uge {g.week} · {g.year}</div>
                                    <div className="wins-list">{(g.winning || []).map((n) => <span key={n} className="win-ball">{n}</span>)}</div>
                                </div>
                            ))}
                        </div>
                    </section>
                )}

                {tab === "setWinners" && (
                    <section>
                        <div className="admin-row">
                            <div className="admin-row__left">
                                <span className="admin-muted">Vælg {WIN_COUNT} vindertal for uge {currentWeek} · {currentYear}</span>
                            </div>
                            <div className="admin-row__right"><span className="admin-muted">{picks.length}/{WIN_COUNT} valgt</span></div>
                        </div>

                        <div className="picker-grid" role="group" aria-label="Vælg vindertal 1–16">
                            {Array.from({ length: 16 }, (_, i) => i + 1).map((n) => {
                                const active = picks.includes(n);
                                return (
                                    <button key={n} type="button" className={`pick-ball ${active ? "pick-ball--active" : ""}`}
                                            onClick={() => togglePick(n)} aria-pressed={active}>
                                        <span>{n}</span>
                                    </button>
                                );
                            })}
                        </div>

                        <div className="picker-actions">
                            <button className="balance-btn" onClick={resetPicks}>Annullér</button>
                            <button className="primary" onClick={saveWinners} disabled={picks.length !== WIN_COUNT}>Gem vindertal</button>
                        </div>
                    </section>
                )}
            </div>

            {showModal && (
                <div className="admin-modal-overlay" onClick={() => setShowModal(false)} role="dialog" aria-modal="true">
                    <div className="admin-modal" onClick={(e) => e.stopPropagation()}>
                        <header className="admin-modal__head">
                            <h3>{editing ? "Rediger spiller" : "Ny spiller"}</h3>
                            <button className="admin-modal__close" onClick={() => setShowModal(false)} aria-label="Luk">×</button>
                        </header>
                        <div className="admin-modal__body">
                            <label className="admin-field">Navn
                                <input className="admin-input" value={form.name} onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))} placeholder="Navn" />
                            </label>
                            <label className="admin-field">E-mail
                                <input className="admin-input" value={form.email} onChange={(e) => setForm((f) => ({ ...f, email: e.target.value }))} placeholder="email@eksempel.dk" />
                            </label>
                            <label className="admin-field">Telefon
                                <input className="admin-input" value={form.phone} onChange={(e) => setForm((f) => ({ ...f, phone: e.target.value }))} placeholder="12 34 56 78" />
                            </label>
                            <label className="admin-switch">
                                <input type="checkbox" checked={form.active} onChange={(e) => setForm((f) => ({ ...f, active: e.target.checked }))} />
                                <span>Aktiv</span>
                            </label>
                        </div>
                        <footer className="admin-modal__foot">
                            <button className="balance-btn" onClick={() => setShowModal(false)}>Annullér</button>
                            <button className="primary" onClick={savePlayer}>{editing ? "Gem" : "Opret"}</button>
                        </footer>
                    </div>
                </div>
            )}
        </div>
    );
}
