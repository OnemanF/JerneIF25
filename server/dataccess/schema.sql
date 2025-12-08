BEGIN;

DROP SCHEMA IF EXISTS public CASCADE;
CREATE SCHEMA public;
SET search_path = public;

CREATE OR REPLACE FUNCTION validate_board_numbers(nums smallint[])
    RETURNS boolean LANGUAGE sql IMMUTABLE AS $$
SELECT nums IS NOT NULL
           AND cardinality(nums) BETWEEN 5 AND 8
           AND (SELECT COUNT(*) = COUNT(DISTINCT n) FROM unnest(nums) t(n))
           AND NOT EXISTS (SELECT 1 FROM unnest(nums) t(n) WHERE n < 1 OR n > 16);
$$;

CREATE TABLE players (
                         id                bigserial PRIMARY KEY,
                         name              varchar(255) NOT NULL,
                         phone             varchar(50),
                         email             varchar(255),
                         is_active         boolean NOT NULL DEFAULT FALSE,
                         member_expires_at date,
                         created_at        timestamptz NOT NULL DEFAULT now(),
                         updated_at        timestamptz,
                         is_deleted        boolean NOT NULL DEFAULT FALSE
);
CREATE INDEX ix_players_active ON players(is_active) WHERE is_deleted = FALSE;
-- (hjælper lookups i admin):
CREATE INDEX IF NOT EXISTS ix_players_email ON players(lower(email)) WHERE email IS NOT NULL AND is_deleted = FALSE;

CREATE TABLE games (
                       id            bigserial PRIMARY KEY,
                       week_start    date NOT NULL UNIQUE,  -- én række pr. uge
                       status        varchar(10) NOT NULL DEFAULT 'inactive' CHECK (status IN ('inactive','active','closed')),
                       winning_nums  smallint[],           -- NULL indtil close; præcis 3 når sat
                       published_at  timestamptz,
                       created_at    timestamptz NOT NULL DEFAULT now(),
                       updated_at    timestamptz,
                       is_deleted    boolean NOT NULL DEFAULT FALSE,
                       CHECK (winning_nums IS NULL OR cardinality(winning_nums) = 3)
);
CREATE INDEX ix_games_status ON games(status) WHERE is_deleted = FALSE;
-- Kun én aktiv uge ad gangen:
CREATE UNIQUE INDEX uq_games_single_active
    ON games ((true))
    WHERE status = 'active' AND is_deleted = FALSE;

CREATE TABLE boards (
                        id            bigserial PRIMARY KEY,
                        game_id       bigint NOT NULL REFERENCES games(id),
                        player_id     bigint NOT NULL REFERENCES players(id),
                        numbers       smallint[] NOT NULL,           -- 5..8, 1..16, distinct
                        price_dkk     numeric(10,2) NOT NULL CHECK (price_dkk IN (20,40,80,160)),
                        purchased_at  timestamptz NOT NULL DEFAULT now(),
                        created_at    timestamptz NOT NULL DEFAULT now(),
                        updated_at    timestamptz,
                        is_deleted    boolean NOT NULL DEFAULT FALSE,
                        CHECK (validate_board_numbers(numbers))
);
CREATE INDEX ix_boards_game   ON boards(game_id)   WHERE is_deleted = FALSE;
CREATE INDEX ix_boards_player ON boards(player_id) WHERE is_deleted = FALSE;

CREATE TABLE board_subscriptions (
                                     id              bigserial PRIMARY KEY,
                                     player_id       bigint NOT NULL REFERENCES players(id),
                                     numbers         smallint[] NOT NULL,
                                     remaining_weeks integer NOT NULL CHECK (remaining_weeks >= 0),
                                     is_active       boolean NOT NULL DEFAULT TRUE,
                                     started_at      timestamptz NOT NULL DEFAULT now(),
                                     canceled_at     timestamptz,
                                     created_at      timestamptz NOT NULL DEFAULT now(),
                                     updated_at      timestamptz,
                                     is_deleted      boolean NOT NULL DEFAULT FALSE,
                                     CHECK (validate_board_numbers(numbers))
);
CREATE INDEX ix_sub_player_active ON board_subscriptions(player_id, is_active) WHERE is_deleted = FALSE;

CREATE TABLE transactions (
                              id            bigserial PRIMARY KEY,
                              player_id     bigint NOT NULL REFERENCES players(id),
                              status        varchar(16) NOT NULL DEFAULT 'pending' CHECK (status IN ('pending','approved','rejected')),
                              amount_dkk    numeric(10,2) NOT NULL CHECK (amount_dkk > 0),
                              mobilepay_ref varchar(64),
                              note          text,
                              requested_at  timestamptz NOT NULL DEFAULT now(),
                              decided_at    timestamptz,
                              created_at    timestamptz NOT NULL DEFAULT now(),
                              updated_at    timestamptz,
                              is_deleted    boolean NOT NULL DEFAULT FALSE
);
CREATE UNIQUE INDEX uq_tx_mobilepay_ref ON transactions(mobilepay_ref)
    WHERE mobilepay_ref IS NOT NULL AND is_deleted = FALSE;
CREATE INDEX ix_tx_player_status ON transactions(player_id, status) WHERE is_deleted = FALSE;

CREATE TABLE IF NOT EXISTS admins (
                                      id            bigserial PRIMARY KEY,
                                      email         varchar(255) NOT NULL UNIQUE,
                                      password_hash text         NOT NULL,
                                      created_at    timestamptz  NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS player_credentials (
                                                  player_id     bigint PRIMARY KEY REFERENCES players(id) ON DELETE CASCADE,
                                                  email         varchar(255) NOT NULL UNIQUE,
                                                  password_hash text         NOT NULL,
                                                  created_at    timestamptz  NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS ix_player_credentials_email ON player_credentials(email);

COMMIT;
