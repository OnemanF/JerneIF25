import { useMemo, useState, useEffect } from "react";
import Footer from "./Footer";
import BalanceModal from "./BalanceModal";
import { http } from "@lib/http";

const PRICES: Record<number, number> = { 5: 20, 6: 40, 7: 80, 8: 160 };

type ActiveGame = { id: number; week_start: string; status: "active"|"closed"|"inactive" };

export default function Play() {
    const [quantity, setQuantity] = useState(1);
    const [currentIndex, setCurrentIndex] = useState(0);
    const [boards, setBoards] = useState<number[][]>([[]]);

    const [repeat, setRepeat] = useState(false);
    const [message, setMessage] = useState<string | null>(null);

    const [balance, setBalance] = useState(500);
    const [showFunds, setShowFunds] = useState(false);

    const [game, setGame] = useState<ActiveGame | null>(null);
    const [blocked, setBlocked] = useState<string | null>(null);
    
    useEffect(() => {
        (async () => {
            try {
                const g = await http.get<ActiveGame>('/games/active');
                setGame(g);
                setBlocked(null);
            } catch {
                setGame(null);
                setBlocked("Ingen aktiv uge endnu.");
            }
        })();
    }, []);

    function syncBoards(newQty: number) {
        setBoards(prev => {
            const next = prev.slice(0, newQty);
            while (next.length < newQty) next.push([]);
            return next;
        });
    }
    function onSetQuantity(n: number) {
        const q = Math.max(1, Math.min(9999, Math.floor(n)));
        setQuantity(q);
        syncBoards(q);
        if (currentIndex > q - 1) setCurrentIndex(q - 1);
    }
    function toggle(n: number) {
        setBoards(prev => {
            const copy = prev.map(b => [...b]);
            const picks = copy[currentIndex] ?? [];
            const i = picks.indexOf(n);
            if (i >= 0) picks.splice(i, 1);
            else {
                if (picks.length >= 8) return prev;
                picks.push(n);
                picks.sort((a,b) => a - b);
            }
            copy[currentIndex] = picks;
            return copy;
        });
    }

    const perBoardPrice = (p: number[]) => PRICES[p.length] ?? 0;
    const perBoardValid = (p: number[]) => p.length >= 5 && p.length <= 8;
    const totalPrice = useMemo(() => boards.reduce((s, b) => s + perBoardPrice(b), 0), [boards]);
    const allValid = boards.every(perBoardValid);
    const canAfford = balance >= totalPrice;

    const canBuy = !!game && !blocked && allValid && totalPrice > 0 && canAfford;

    async function buy() {
        if (!canBuy || !game) return;
        try {
            for (const nums of boards) {
                await http.post('/boards', {
                    numbers: nums,
                    gameId: game.id,
                    repeatGames: repeat ? 4 : 0,
                });
            }
            setBalance(b => b - totalPrice);
            setMessage(`🎉 Købt ${boards.length} kupon${boards.length > 1 ? "er" : ""} – ${totalPrice} DKK`);
            setBoards(Array.from({ length: quantity }, () => []));
            setRepeat(false);
            setTimeout(() => setMessage(null), 3500);
        } catch (e: any) {
            const msg = String(e?.message ?? '');
            if (msg.includes('Køb lukket') || msg.includes('Cutoff')) {
                setBlocked('Køb lukket for denne uge (efter lørdag kl. 17).');
            } else {
                setBlocked('Køb blokeret.');
            }
        }
    }

    const current = boards[currentIndex] ?? [];

    return (
        <div className="page lotto-page" style={{ paddingBottom: 96 }}>
            <header className="lotto-hero">
                <div className="lotto-hero__title">Ugens Lotto</div>
                <div className="lotto-hero__meta">
                    {blocked ?? (game ? "Åben for køb" : "Indlæser...")}
                </div>
            </header>

            <section className="lotto-card">
                <h2>Kupon {currentIndex + 1} / {quantity}</h2>
                <p className="lotto-muted">Vælg <b>5–8</b> kugler (1–16).</p>

                <div className="ball-grid" role="group" aria-label="Vælg tal til denne kupon">
                    {Array.from({ length: 16 }, (_, i) => i + 1).map((n) => {
                        const active = current.includes(n);
                        return (
                            <button
                                key={n}
                                type="button"
                                className={`ball ${active ? "ball--picked" : ""}`}
                                onClick={() => toggle(n)}
                                aria-pressed={active}
                            >
                                <span>{n}</span>
                            </button>
                        );
                    })}
                </div>

                <div className="lotto-controls">
                    <div className="lotto-summary">
                        <span className="chip">{current.length} valgt</span>
                        <span className="chip chip--price">{perBoardPrice(current)} DKK</span>
                    </div>
                </div>
            </section>

            {message && (
                <div className="lotto-toast" role="status" aria-live="polite">
                    {message}
                    <button className="toast-close" onClick={() => setMessage(null)} aria-label="Luk">×</button>
                </div>
            )}

            <Footer
                currentIndex={currentIndex}
                setCurrentIndex={setCurrentIndex}
                couponCount={quantity}
                setCouponCount={onSetQuantity}
                repeating={repeat}
                setRepeating={setRepeat}
                totalPriceDkk={totalPrice}
                balanceDkk={balance}
                onOpenBalance={() => setShowFunds(true)}
                onBuy={buy}
                canBuy={canBuy}
            />

            <BalanceModal
                visible={showFunds}
                onClose={() => setShowFunds(false)}
                onDeposit={(a) => setBalance(b => b + a)}
                onWithdraw={(a) => setBalance(b => Math.max(0, b - a))}
                balance={balance}
            />
        </div>
    );
}
