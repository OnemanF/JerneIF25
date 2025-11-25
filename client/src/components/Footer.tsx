import "./Footer.css"; 
import { useEffect, useMemo, useState } from "react";

type Props = {
    currentIndex: number;
    setCurrentIndex: (i: number) => void;
    couponCount: number;
    setCouponCount: (n: number) => void;
    repeating: boolean;
    setRepeating: (b: boolean) => void;
    totalPriceDkk: number;
    balanceDkk: number;
    onOpenBalance: () => void;
    onBuy: () => void;
    canBuy?: boolean;
};

export default function Footer({
                                   currentIndex,
                                   setCurrentIndex,
                                   couponCount,
                                   setCouponCount,
                                   repeating,
                                   setRepeating,
                                   totalPriceDkk,
                                   balanceDkk,
                                   onOpenBalance,
                                   onBuy,
                                   canBuy = true,
                               }: Props) {
    const [countStr, setCountStr] = useState(String(couponCount));
    useEffect(() => setCountStr(String(couponCount)), [couponCount]);

    const sanitized = useMemo(() => {
        const n = parseInt(countStr, 10);
        if (Number.isNaN(n)) return 0;
        return Math.min(Math.max(n, 0), 9999);
    }, [countStr]);

    function normalize() {
        const n = sanitized <= 0 ? 1 : sanitized;
        setCouponCount(n);
        setCountStr(String(n));
        if (currentIndex > n - 1) setCurrentIndex(Math.max(0, n - 1));
    }
    function inc() {
        const n = (sanitized || 0) + 1;
        setCouponCount(n);
        setCountStr(String(n));
    }
    function dec() {
        const n = Math.max((sanitized || 1) - 1, 1);
        setCouponCount(n);
        setCountStr(String(n));
        if (currentIndex > n - 1) setCurrentIndex(n - 1);
    }

    return (
        <footer className="lotto-footer">
            <div className="lotto-footer__inner">
                <div className="lotto-footer__left">
                    <span className="lf-label">Antal kuponer</span>

                    <div className="qty-ctrl">
                        <button className="qty-btn" onClick={dec} aria-label="Færre">−</button>
                        <input
                            className="qty-input"
                            type="text"
                            inputMode="numeric"
                            pattern="[0-9]*"
                            value={countStr}
                            onChange={(e) => setCountStr(e.target.value.replace(/\D+/g, ""))}
                            onBlur={normalize}
                            onKeyDown={(e) => { if (e.key === "Enter") (e.target as HTMLInputElement).blur(); }}
                            placeholder="1"
                            aria-label="Antal kuponer"
                        />
                        <button className="qty-btn" onClick={inc} aria-label="Flere">+</button>
                    </div>

                    <div className="nav">
                        <button
                            className="nav-btn"
                            onClick={() => setCurrentIndex(Math.max(currentIndex - 1, 0))}
                            aria-label="Forrige kupon"
                            disabled={currentIndex === 0}
                        >
                            ◀
                        </button>

                        <span className="nav-indicator">
              Kupon {currentIndex + 1} / {Math.max(sanitized, 1) || 1}
            </span>

                        <button
                            className="nav-btn"
                            onClick={() =>
                                setCurrentIndex(Math.min(currentIndex + 1, Math.max(sanitized, 1) - 1))
                            }
                            aria-label="Næste kupon"
                            disabled={currentIndex >= Math.max(sanitized, 1) - 1}
                        >
                            ▶
                        </button>
                    </div>
                </div>

                <div className="lotto-footer__right">
                    <label className="switch">
                        <input
                            type="checkbox"
                            checked={repeating}
                            onChange={(e) => setRepeating(e.target.checked)}
                        />
                        <span className="switch-track" />
                        <span className="switch-label">Gentag hver uge</span>
                    </label>

                    <span className="total">Total: <b>{totalPriceDkk} DKK</b></span>

                    <div className="actions">
                        {}
                        <button
                            className="balance-btn"
                            onClick={onOpenBalance}
                            title={`Saldo: ${balanceDkk} DKK`}
                            aria-label="Åbn saldo"
                        >
                            Saldo
                        </button>

                        <button className="primary" onClick={onBuy} disabled={!canBuy}>
                            Køb kupon(er)
                        </button>
                    </div>
                </div>
            </div>
        </footer>
    );
}