import { useMemo, useState } from "react";
import Footer from "./Footer";
import BalanceModal from "./BalanceModal";

const PRICES: Record<number, number> = { 5: 20, 6: 40, 7: 80, 8: 160 };

function getISOWeek(d: Date) {
    const date = new Date(Date.UTC(d.getFullYear(), d.getMonth(), d.getDate()));
    const dayNum = (date.getUTCDay() || 7);
    date.setUTCDate(date.getUTCDate() + 4 - dayNum);
    const yearStart = new Date(Date.UTC(date.getUTCFullYear(), 0, 1));
    return Math.ceil((((date as any) - (yearStart as any)) / 86400000 + 1) / 7);
}

export default function Play() {
    const [quantity, setQuantity] = useState(1);
    const [currentIndex, setCurrentIndex] = useState(0);
    const [boards, setBoards] = useState<number[][]>([[]]);
    
    const [repeat, setRepeat] = useState(false);
    const [message, setMessage] = useState<string | null>(null);
    
    const [balance, setBalance] = useState(500);
    const [showFunds, setShowFunds] = useState(false);
    
    function syncBoards(newQty: number) {
        setBoards((prev) => {
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
        setBoards((prev) => {
            const copy = prev.map((b) => [...b]);
            const picks = copy[currentIndex] ?? [];
            const i = picks.indexOf(n);
            if (i >= 0) picks.splice(i, 1);
            else {
                if (picks.length >= 8) return prev;
                picks.push(n);
                picks.sort((a, b) => a - b);
            }
            copy[currentIndex] = picks;
            return copy;
        });
    }

    const perBoardPrice = (p: number[]) => PRICES[p.length] ?? 0;
    const perBoardValid = (p: number[]) => p.length >= 5 && p.length <= 8;

    const totalPrice = useMemo(
        () => boards.reduce((sum, b) => sum + perBoardPrice(b), 0),
        [boards]
    );
    const allValid = boards.every(perBoardValid);
    const canAfford = balance >= totalPrice;
    const canBuy = allValid && totalPrice > 0 && canAfford; // no auth check

    function buy() {
        if (!canBuy) return;
        setBalance((b) => b - totalPrice);
        setMessage(`🎉 Købt ${boards.length} kupon${boards.length > 1 ? "er" : ""} – ${totalPrice} DKK`);
        setBoards(Array.from({ length: quantity }, () => []));
        setRepeat(false);
        setTimeout(() => setMessage(null), 3500);
    }
    
    function deposit(amount: number) { setBalance((b) => b + amount); }
    function withdraw(amount: number) { setBalance((b) => Math.max(0, b - amount)); }

    const now = new Date();
    const week = getISOWeek(now);
    const year = now.getFullYear();
    const current = boards[currentIndex] ?? [];

    return (
        <div className="page lotto-page" style={{ paddingBottom: 96 }}>
            <header className="lotto-hero">
                <div className="lotto-hero__title">Ugens Lotto</div>
                <div className="lotto-hero__meta">Uge {week} · {year}</div>
            </header>

            <section className="lotto-card">
                <h2>Kupon {currentIndex + 1} / {quantity}</h2>
                <p className="lotto-muted">Hver kupon har sine egne tal. Vælg <b>5–8</b> kugler (1–16).</p>

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
                onDeposit={deposit}
                onWithdraw={withdraw}
                balance={balance}
            />
        </div>
    );
}