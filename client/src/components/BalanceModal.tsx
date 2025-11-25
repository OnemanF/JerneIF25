import React, { useState } from "react";
import "./BalanceModal.css";

type Props = {
    visible: boolean;
    onClose: () => void;
    onDeposit: (amount: number) => void;
    onWithdraw: (amount: number) => void;
    balance: number;
};

export default function BalanceModal({
                                         visible,
                                         onClose,
                                         onDeposit,
                                         onWithdraw,
                                         balance,
                                     }: Props) {
    const [tab, setTab] = useState<"deposit" | "withdraw">("deposit");
    const [amount, setAmount] = useState<number>(100);

    if (!visible) return null;

    function act() {
        if (amount <= 0 || !Number.isFinite(amount)) return;
        if (tab === "deposit") onDeposit(amount);
        else onWithdraw(amount);
        setAmount(100);
        onClose();
    }

    return (
        <div className="modal-overlay" onClick={onClose} role="dialog" aria-modal="true">
            <div className="modal" onClick={(e) => e.stopPropagation()}>
                <header className="modal-header">
                    <h3>Saldo</h3>
                    <button className="modal-close" onClick={onClose} aria-label="Luk">×</button>
                </header>

                <div className="modal-balance">
                    Nu: <b>{balance} DKK</b>
                </div>

                <div className="tabs" role="tablist" aria-label="Saldo handling">
                    <button
                        className={`tab ${tab === "deposit" ? "tab--active" : ""}`}
                        onClick={() => setTab("deposit")}
                        role="tab"
                        aria-selected={tab === "deposit"}
                    >
                        Indbetal
                    </button>
                    <button
                        className={`tab ${tab === "withdraw" ? "tab--active" : ""}`}
                        onClick={() => setTab("withdraw")}
                        role="tab"
                        aria-selected={tab === "withdraw"}
                    >
                        Udbetal
                    </button>
                </div>

                <div className="modal-body">
                    <label className="field">
                        Beløb (DKK)
                        <input
                            className="field-input"
                            type="number"
                            min={1}
                            value={amount}
                            onChange={(e) => setAmount(parseInt(e.target.value || "0", 10))}
                        />
                    </label>

                    <div className="quick">
                        Hurtig:
                        {[100, 200, 500, 1000].map((n) => (
                            <button key={n} className="chip-btn" onClick={() => setAmount(n)}>
                                {n}
                            </button>
                        ))}
                    </div>
                </div>

                <footer className="modal-footer">
                    <button className="ghost" onClick={onClose}>Annullér</button>
                    <button className="primary" onClick={act}>
                        {tab === "deposit" ? "Indbetal" : "Udbetal"}
                    </button>
                </footer>
            </div>
        </div>
    );
}
