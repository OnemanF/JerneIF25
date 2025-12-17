import { useEffect, useMemo, useRef, useState } from "react";
import "./AdminDashboard.css";
import "../Footer.css";
import ThisWeekWinners from "./ThisWeekWinners";
import { http } from "@lib/http";

function pickArray<T = any>(x: any): T[] {
    if (Array.isArray(x)) return x as T[];
    if (x && Array.isArray(x.$values)) return x.$values as T[];
    return [];
}

function getISOWeek(d: Date) {
    const date = new Date(Date.UTC(d.getFullYear(), d.getMonth(), d.getDate()));
    const dayNum = date.getUTCDay() || 7;
    date.setUTCDate(date.getUTCDate() + 4 - dayNum);
    const yearStart = new Date(Date.UTC(date.getUTCFullYear(), 0, 1));
    return Math.ceil((((date as any) - (yearStart as any)) / 86400000 + 1) / 7);
}
function toWeekYear(week_start?: string) {
    if (!week_start) {
        const now = new Date();
        return { week: getISOWeek(now), year: now.getFullYear() };
    }
    const d = new Date(week_start + "T00:00:00Z");
    return { week: getISOWeek(d), year: d.getUTCFullYear() };
}

type PlayerDTO = { id: number; name: string; email: string; phone?: string | null; is_active?: boolean; active?: boolean };
type PlayerUI  = { id: number; name: string; email: string; phone?: string; active: boolean };

type GameDTO = {
    id: number;
    week_start: string; 
    status: "inactive" | "active" | "closed";
    winning?: number[] | null;
    winning_nums?: number[] | null;
    revenueDkk?: number;
};
type GameUI = { id: number; week_start?: string; status?: string; winning: number[]; revenueDkk: number };

type Tx = {
    id: number;
    player_id: number;
    amount_dkk: number;
    status: "pending" | "approved" | "rejected" | string;
    mobilepay_ref?: string | null;
    requested_at?: string;
};

type BoardRow = {
    id: number;
    playerId: number;
    playerName: string;
    priceDkk: number;
    numbers: number[];
    isWinner: boolean;
};
type GameBoardsResponse = {
    gameId: number;
    winnersTotal: number;
    boardsTotal: number;
    boards: BoardRow[] | { $values: BoardRow[] } | null;
};

const WIN_COUNT = 3;

function mapPlayer(p: PlayerDTO): PlayerUI {
    return {
        id: p.id,
        name: p.name,
        email: p.email,
        phone: p.phone ?? undefined,
        active: (p.is_active ?? p.active ?? false) as boolean,
    };
}
function mapGame(g: GameDTO): GameUI {
    const nums = pickArray<number>(g.winning ?? g.winning_nums);
    return {
        id: g.id,
        week_start: g.week_start,
        status: g.status,
        winning: nums,
        revenueDkk: g.revenueDkk ?? 0,
    };
}

export default function AdminDashboard() {
    const [tab, setTab] = useState<"history" | "players" | "winners" | "setWinners" | "transactions">("history");
    
    const [players, setPlayers] = useState<PlayerUI[]>([]);
    const [playersNote, setPlayersNote] = useState<string | null>(null);
    
    const [activeGame, setActiveGame] = useState<GameUI | null>(null);
    const [closedGames, setClosedGames] = useState<GameUI[]>([]);
    
    const [txs, setTxs] = useState<Tx[]>([]);
    
    const [loading, setLoading] = useState(false);
    const [query, setQuery] = useState("");
    const [sortDesc, setSortDesc] = useState(true);
    
    const now = new Date();
    const [currentWeek, setCurrentWeek] = useState(getISOWeek(now));
    const [currentYear, setCurrentYear] = useState(now.getFullYear());
    
    const [picks, setPicks] = useState<number[]>([]);
    
    const booted = useRef(false);
    
    const [openGameId, setOpenGameId] = useState<number | null>(null);
    const [boardSheets, setBoardSheets] = useState<Record<number, GameBoardsResponse>>({});
    const [boardsLoading, setBoardsLoading] = useState(false);
    
    async function fetchPlayers() {
        setPlayersNote(null);
        try {
            const res = await http.get<any>("/players");
            const arr = pickArray<PlayerDTO>(res).map(mapPlayer);
            setPlayers(arr);
            if (arr.length === 0) setPlayersNote("Ingen spillere fundet.");
        } catch (e: any) {
            setPlayers([]);
            setPlayersNote(e?.message?.toString?.() ?? "Kunne ikke hente spillere.");
        }
    }

    async function fetchActiveGame() {
        try {
            const g = await http.get<GameDTO>("/games/active");
            if (g?.id) {
                const mapped = mapGame(g);
                setActiveGame(mapped);
                setPicks(mapped.winning.slice(0, WIN_COUNT)); // preload any draft
            } else {
                setActiveGame(null);
                setPicks([]);
            }
        } catch {
            setActiveGame(null);
            setPicks([]);
        }
    }

    async function fetchClosedHistory() {
        try {
            const list = await http.get<any>("/games?status=closed");
            setClosedGames(pickArray<GameDTO>(list).map(mapGame));
        } catch {
            setClosedGames([]);
        }
    }

    async function fetchTransactions() {
        try {
            const all = await http.get<any>("/transactions?filters=status==pending");
            setTxs(pickArray<Tx>(all));
        } catch {
            setTxs([]);
        }
    }

    async function fetchBoardsFor(gameId: number) {
        if (boardSheets[gameId]) return;
        setBoardsLoading(true);
        try {
            const res = await http.get<GameBoardsResponse>(`/games/${gameId}/boards`);
            setBoardSheets(prev => ({ ...prev, [gameId]: res }));
        } finally {
            setBoardsLoading(false);
        }
    }

    async function bootstrap() {
        setLoading(true);
        try {
            await Promise.all([fetchPlayers(), fetchActiveGame(), fetchClosedHistory(), fetchTransactions()]);
        } finally {
            setLoading(false);
        }
    }

    useEffect(() => {
        if (booted.current) return;
        booted.current = true;
        const n = new Date();
        setCurrentWeek(getISOWeek(n));
        setCurrentYear(n.getFullYear());
        bootstrap();
    }, []);
    
    const combinedGames: GameUI[] = useMemo(() => {
        const arr: GameUI[] = [];
        if (activeGame) arr.push(activeGame);
        arr.push(...closedGames);
        const uniq = Array.from(new Map(arr.map(g => [g.id, g])).values());
        return uniq.sort((a, b) => {
            const ka = a.week_start ?? "";
            const kb = b.week_start ?? "";
            return sortDesc ? kb.localeCompare(ka) : ka.localeCompare(kb);
        });
    }, [activeGame, closedGames, sortDesc]);
    
    const targetGame = activeGame && activeGame.status === "active" ? activeGame : null;
    const hasActive = !!targetGame;
    const canPublish = hasActive && picks.length === WIN_COUNT;
    
    useEffect(() => {
        setPicks((targetGame?.winning ?? []).slice(0, WIN_COUNT));
    }, [targetGame?.id]);
    
    const [saving, setSaving] = useState(false);
    const [saveErr, setSaveErr] = useState<string | null>(null);

    async function savePlayer(editing?: PlayerUI, form?: { name: string; email: string; phone: string; active: boolean }) {
        if (!form) return;
        if (!form.name.trim() || !form.email.trim()) return;

        setSaving(true);
        setSaveErr(null);
        try {
            if (editing) {
                await http.put(`/players/${editing.id}`, {
                    Name: form.name.trim(),
                    Email: form.email.trim(),
                    Phone: form.phone.trim(),
                    IsActive: form.active,
                    MemberExpiresAt: null,
                });
            } else {
                await http.post(`/players`, {
                    Name: form.name.trim(),
                    Email: form.email.trim(),
                    Phone: form.phone.trim(),
                });
            }
            await fetchPlayers();
            setShowModal(false);
            setEditing(null);
        } catch (e: any) {
            setSaveErr(e?.message ?? "Kunne ikke gemme spilleren.");
        } finally {
            setSaving(false);
        }
    }

    async function toggleActive(id: number) {
        const p = players.find(x => x.id === id);
        if (!p) return;
        await http.put(`/players/${id}`, {
            Name: p.name,
            Email: p.email,
            Phone: p.phone || "",
            IsActive: !p.active,
            MemberExpiresAt: null,
        });
        await fetchPlayers();
    }

    async function deletePlayer(id: number) {
        if (!confirm("Slet spiller? Dette kan ikke fortrydes.")) return;
        await http.del(`/players/${id}`);
        await fetchPlayers();
    }
    
    function togglePick(n: number) {
        setPicks(prev =>
            prev.includes(n)
                ? prev.filter(x => x !== n)
                : prev.length >= WIN_COUNT
                    ? prev
                    : [...prev, n].sort((a, b) => a - b)
        );
    }

    async function saveWinners() {
        if (!hasActive) {
            alert(
                "Ingen aktiv uge.\nDu kan vælge vindertal, når en ny uge starter. " +
                "Systemet opretter automatisk den nye uge."
            );
            return;
        }
        if (!targetGame || picks.length !== WIN_COUNT) return;

        const wy = toWeekYear(targetGame.week_start);
        const ok = confirm(`Bekræft vindertal for uge ${wy.week} · ${wy.year}: ${picks.join(", ")}`);
        if (!ok) return;

        try {
            await http.post<GameDTO>("/games/publish", { gameId: targetGame.id, numbers: picks });
            await Promise.all([fetchActiveGame(), fetchClosedHistory()]);
            setTab("winners");
        } catch (e: any) {
            alert("Kunne ikke gemme vindertal: " + (e?.message ?? ""));
        }
    }

    // Rollback
    async function undoLast() {
        const newestClosed =
            closedGames.slice().sort((a, b) => (b.week_start || "").localeCompare(a.week_start || ""))[0] || null;

        if (!newestClosed) {
            alert("Ingen lukket uge at fortryde.");
            return;
        }

        const wy = toWeekYear(newestClosed.week_start);
        const ok = confirm(`Fortryd sidste udtræk (uge ${wy.week} · ${wy.year})?`);
        if (!ok) return;

        try {
            await http.post("/games/undo", { closedGameId: newestClosed.id });
            await Promise.all([fetchActiveGame(), fetchClosedHistory()]);
            setPicks([]);
            alert("Fortryd udført. Ugen er aktiv igen.");
        } catch (e: any) {
            alert("Kunne ikke fortryde: " + (e?.message ?? ""));
        }
    }

    function resetPicks() {
        setPicks((targetGame?.winning ?? []).slice(0, WIN_COUNT));
    }
    
    async function approveTx(id: number) {
        await http.post("/transactions/decide", { transactionId: id, decision: "approve" });
        await fetchTransactions();
    }
    async function rejectTx(id: number) {
        await http.post("/transactions/decide", { transactionId: id, decision: "reject" });
        await fetchTransactions();
    }
    
    const [showModal, setShowModal] = useState(false);
    const [editing, setEditing] = useState<PlayerUI | null>(null);
    const [form, setForm] = useState<{ name: string; email: string; phone: string; active: boolean }>({
        name: "",
        email: "",
        phone: "",
        active: true,
    });

    function openNew() {
        setEditing(null);
        setForm({ name: "", email: "", phone: "", active: true });
        setSaveErr(null);
        setShowModal(true);
    }
    function openEdit(p: PlayerUI) {
        setEditing(p);
        setForm({ name: p.name, email: p.email, phone: p.phone || "", active: p.active });
        setSaveErr(null);
        setShowModal(true);
    }
    
    return (
        <div className="page admin-page" style={{ paddingBottom: 40 }}>
            <header className="admin-hero">
                <div className="admin-hero__title">Admin</div>
                <div className="admin-hero__meta">Dashboard {loading && "· indlæser…"}</div>
            </header>

            <div className="admin-card">
                <div className="admin-tabs" role="tablist" aria-label="Admin faner">
                    <button className={`admin-tab ${tab === "history" ? "is-active" : ""}`} onClick={() => setTab("history")}>Historik</button>
                    <button className={`admin-tab ${tab === "players" ? "is-active" : ""}`} onClick={() => setTab("players")}>Spillere</button>
                    <button className={`admin-tab ${tab === "winners" ? "is-active" : ""}`} onClick={() => setTab("winners")}>Vindertal</button>
                    <button className={`admin-tab ${tab === "setWinners" ? "is-active" : ""}`} onClick={() => setTab("setWinners")}>Sæt vindertal</button>
                    <button className={`admin-tab ${tab === "transactions" ? "is-active" : ""}`} onClick={() => setTab("transactions")}>Indbetalinger</button>
                </div>

                {tab === "history" && (
                    <section>
                        <div className="admin-row">
                            <div className="admin-row__left"><span className="admin-muted">Aktiv uge + lukkede uger</span></div>
                            <div className="admin-row__right">
                                <button className="balance-btn" onClick={() => setSortDesc(s => !s)}>
                                    Sortér: {sortDesc ? "Nyeste først" : "Ældste først"}
                                </button>
                            </div>
                        </div>

                        <div className="admin-table">
                            <div className="admin-thead">
                                <div>Uge</div><div>År</div><div>Status</div><div>Omsætning</div><div>Vindertal</div><div></div>
                            </div>

                            {combinedGames.map((g) => {
                                const wy = toWeekYear(g.week_start);
                                const nums = pickArray<number>(g.winning);
                                const isOpen = openGameId === g.id;
                                const sheet = boardSheets[g.id!];
                                const rows = pickArray<BoardRow>(sheet?.boards);

                                return (
                                    <div key={g.id} style={{ display: "contents" }}>
                                        <div className="admin-trow">
                                            <div>{wy.week}</div>
                                            <div>{wy.year}</div>
                                            <div>{g.status || "-"}</div>
                                            <div>{g.revenueDkk ?? 0} DKK</div>
                                            <div className="wins">{nums.map(n => <span key={n} className="win-ball">{n}</span>)}</div>
                                            <div style={{ justifySelf: "end" }}>
                                                <button
                                                    className="balance-btn"
                                                    onClick={() => {
                                                        const next = isOpen ? null : g.id!;
                                                        setOpenGameId(next);
                                                        if (next) fetchBoardsFor(next);
                                                    }}
                                                >
                                                    {isOpen ? "Skjul" : "Vis brætter"}
                                                </button>
                                            </div>
                                        </div>

                                        {isOpen && (
                                            <div className="admin-subtable" style={{ gridColumn: "1 / -1", marginTop: 8 }}>
                                                {!sheet && (boardsLoading ? <div className="admin-muted">Indlæser…</div> : <div className="admin-muted">Ingen brætdata.</div>)}
                                                {sheet && (
                                                    <>
                                                        <div className="admin-row" style={{ marginBottom: 6 }}>
                                                            <div className="admin-muted">
                                                                {sheet.boardsTotal} bræt · {sheet.winnersTotal} vinder-bræt
                                                            </div>
                                                        </div>
                                                        <div className="admin-table">
                                                            <div className="admin-thead">
                                                                <div>Bræt #</div>
                                                                <div>Spiller</div>
                                                                <div>Tal</div>
                                                                <div>Pris</div>
                                                                <div>Vinder</div>
                                                            </div>
                                                            {rows.map(b => {
                                                                const winNums = nums;
                                                                return (
                                                                    <div className="admin-trow" key={b.id}>
                                                                        <div>{b.id}</div>
                                                                        <div>{b.playerName}</div>
                                                                        <div>
                                                                            <div className="wins" style={{ gap: 6 }}>
                                                                                {pickArray<number>(b.numbers).map(n => (
                                                                                    <span
                                                                                        key={n}
                                                                                        className={`win-ball ${winNums.includes(n) ? "" : "win-ball--muted"}`}
                                                                                        title={winNums.includes(n) ? "Match" : ""}
                                                                                    >
                                            {n}
                                          </span>
                                                                                ))}
                                                                            </div>
                                                                        </div>
                                                                        <div>{b.priceDkk} DKK</div>
                                                                        <div>
                                      <span className={`chip ${b.isWinner ? "chip--ok" : "chip--muted"}`}>
                                        {b.isWinner ? "Vinder" : "–"}
                                      </span>
                                                                        </div>
                                                                    </div>
                                                                );
                                                            })}
                                                            {rows.length === 0 && (
                                                                <div className="admin-trow"><div>Ingen brætter.</div></div>
                                                            )}
                                                        </div>
                                                    </>
                                                )}
                                            </div>
                                        )}
                                    </div>
                                );
                            })}

                            {combinedGames.length === 0 && (
                                <div className="admin-trow"><div>–</div><div>–</div><div>Ingen data</div><div>–</div><div>–</div></div>
                            )}
                        </div>
                    </section>
                )}

                {tab === "players" && (
                    <section>
                        <div className="admin-row">
                            <div className="admin-row__left">
                                <input
                                    className="admin-input"
                                    placeholder="Søg spiller (navn, e-mail, tlf.)"
                                    value={query}
                                    onChange={(e) => setQuery(e.target.value)}
                                />
                            </div>
                            <div className="admin-row__right">
                                <button className="primary" onClick={openNew}>Ny spiller</button>
                            </div>
                        </div>
                        {playersNote && <div className="admin-muted" style={{ marginBottom: 8 }}>{playersNote}</div>}
                        <div className="admin-table">
                            <div className="admin-thead"><div>Navn</div><div>E-mail</div><div>Telefon</div><div>Status</div><div>Handling</div></div>
                            {players
                                .filter(p => {
                                    const q = query.trim().toLowerCase();
                                    if (!q) return true;
                                    return p.name.toLowerCase().includes(q) || p.email.toLowerCase().includes(q) || (p.phone || "").toLowerCase().includes(q);
                                })
                                .map(p => (
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
                            {players.length === 0 && <div className="admin-trow"><div>Ingen spillere.</div></div>}
                        </div>
                    </section>
                )}

                {tab === "winners" && (
                    <section>
                        <ThisWeekWinners
                            games={combinedGames.map(g => {
                                const wy = toWeekYear(g.week_start);
                                return { id: g.id!, week: wy.week, year: wy.year, winning: pickArray<number>(g.winning) };
                            })}
                            week={currentWeek}
                            year={currentYear}
                        />
                        <p className="admin-muted">Tidligere vindertal (seneste først)</p>
                        <div className="wins-grid">
                            {combinedGames.map(g => {
                                const wy = toWeekYear(g.week_start);
                                const nums = pickArray<number>(g.winning);
                                return (
                                    <div key={g.id} className="wins-card">
                                        <div className="wins-head">Uge {wy.week} · {wy.year}</div>
                                        <div className="wins-list">{nums.map(n => <span key={n} className="win-ball">{n}</span>)}</div>
                                    </div>
                                );
                            })}
                        </div>
                    </section>
                )}

                {tab === "setWinners" && (
                    <section>
                        <div className="admin-row">
                            <div className="admin-row__left">
                <span className="admin-muted">
                  {hasActive
                      ? (() => {
                          const wy = toWeekYear(targetGame!.week_start);
                          return `Vælg ${WIN_COUNT} vindertal for uge ${wy.week} · ${wy.year}`;
                      })()
                      : "Ingen aktiv uge — den oprettes automatisk efter sidste udtræk."}
                </span>
                            </div>
                            <div className="admin-row__right" style={{ display: "flex", gap: 8, alignItems: "center" }}>
                                <button className="danger-btn" onClick={undoLast}>Fortryd sidste udtræk</button>
                                <span className="admin-muted">{picks.length}/{WIN_COUNT} valgt</span>
                            </div>
                        </div>

                        <div className="picker-grid" role="group" aria-label="Vælg vindertal 1–16">
                            {Array.from({ length: 16 }, (_, i) => i + 1).map((n) => {
                                const active = picks.includes(n);
                                return (
                                    <button
                                        key={n}
                                        type="button"
                                        className={`pick-ball ${active ? "pick-ball--active" : ""}`}
                                        onClick={() => togglePick(n)}
                                        aria-pressed={active}
                                        disabled={!hasActive}
                                    >
                                        <span>{n}</span>
                                    </button>
                                );
                            })}
                        </div>

                        <div className="picker-actions">
                            <button className="balance-btn" onClick={resetPicks} disabled={!hasActive}>Annullér</button>
                            <button className="primary" onClick={saveWinners} disabled={!canPublish}>Gem vindertal</button>
                        </div>
                    </section>
                )}

                {tab === "transactions" && (
                    <section>
                        <div className="admin-row">
                            <div className="admin-row__left"><span className="admin-muted">Afventer godkendelse</span></div>
                            <div className="admin-row__right"><button className="balance-btn" onClick={fetchTransactions}>Opdater</button></div>
                        </div>

                        <div className="admin-table">
                            <div className="admin-thead"><div>ID</div><div>Spiller</div><div>Beløb</div><div>Status</div><div>Handling</div></div>
                            {txs.map(tx => {
                                const p = players.find(x => x.id === tx.player_id);
                                return (
                                    <div className="admin-trow" key={tx.id}>
                                        <div>{tx.id}</div>
                                        <div>{p ? `${p.name} (${p.email})` : tx.player_id}</div>
                                        <div>{tx.amount_dkk} DKK</div>
                                        <div><span className="chip chip--muted">{tx.status}</span></div>
                                        <div className="admin-actions">
                                            <button className="balance-btn" onClick={() => approveTx(tx.id)}>Godkend</button>
                                            <button className="danger-btn" onClick={() => rejectTx(tx.id)}>Afvis</button>
                                        </div>
                                    </div>
                                );
                            })}
                            {txs.length === 0 && <div className="admin-trow"><div>Ingen afventende indbetalinger.</div></div>}
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
                            {saveErr && <div className="admin-muted" style={{ color: "#f88", marginBottom: 8 }}>{saveErr}</div>}
                            <label className="admin-field">Navn
                                <input className="admin-input" value={form.name} onChange={(e) => setForm(f => ({ ...f, name: e.target.value }))} placeholder="Navn" />
                            </label>
                            <label className="admin-field">E-mail
                                <input className="admin-input" value={form.email} onChange={(e) => setForm(f => ({ ...f, email: e.target.value }))} placeholder="email@eksempel.dk" />
                            </label>
                            <label className="admin-field">Telefon
                                <input className="admin-input" value={form.phone} onChange={(e) => setForm(f => ({ ...f, phone: e.target.value }))} placeholder="12 34 56 78" />
                            </label>
                            <label className="admin-switch">
                                <input type="checkbox" checked={form.active} onChange={(e) => setForm(f => ({ ...f, active: e.target.checked }))} />
                                <span>Aktiv</span>
                            </label>
                        </div>
                        <footer className="admin-modal__foot">
                            <button className="balance-btn" onClick={() => setShowModal(false)} disabled={saving}>Annullér</button>
                            <button
                                className="primary"
                                onClick={() => savePlayer(editing ?? undefined, form)}
                                disabled={saving || !form.name.trim() || !form.email.trim()}
                                aria-busy={saving}
                            >
                                {saving ? "Gemmer…" : editing ? "Gem" : "Opret"}
                            </button>
                        </footer>
                    </div>
                </div>
            )}
        </div>
    );
}
