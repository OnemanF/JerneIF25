import React from "react";

export type Game = { id: number; week: number; year: number; winning?: number[] };

export default function ThisWeekWinners({
                                            games,
                                            week,
                                            year,
                                        }: {
    games: Game[];
    week: number;
    year: number;
}) {
    const current = games.find((g) => g.week === week && g.year === year);
    return (
        <div className="wins-current">
            <div className="wins-current__head">Denne uge · Uge {week} · {year}</div>
            <div className="wins-list">
                {current?.winning?.length ? (
                    current.winning.map((n) => <span key={n} className="win-ball">{n}</span>)
                ) : (
                    <span className="admin-muted">Ikke trukket endnu</span>
                )}
            </div>
        </div>
    );
}
