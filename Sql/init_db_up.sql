CREATE TABLE "node_casper_blocks"
(
    "hash"      VARCHAR(64) PRIMARY KEY,
    "era"       BIGINT      NOT NULL,
    "timestamp" timestamptz NOT NULL,
    "height"    BIGINT      NOT NULL,
    "era_end"   bool        NOT NULL,
    "validated" bool        NOT NULL
);

CREATE TABLE "node_casper_raw_blocks"
(
    "hash" VARCHAR(64) PRIMARY KEY,
    "data" jsonb NOT NULL
);

CREATE TABLE "node_casper_deploys"
(
    "hash"          VARCHAR(64) PRIMARY KEY,
    "from"          VARCHAR(68) NOT NULL,
    "cost"          VARCHAR     NOT NULL,
    "result"        boolean     NOT NULL,
    "timestamp"     timestamptz NOT NULL,
    "block"         VARCHAR(64) NOT NULL,
    "type"          VARCHAR     NOT NULL,
    "metadata_type" VARCHAR     NOT NULL,
    "contract_hash" VARCHAR(64),
    "contract_name" VARCHAR,
    "entrypoint"    VARCHAR,
    "metadata"      jsonb,
    "events"        jsonb
);

CREATE TABLE "node_casper_raw_deploys"
(
    "hash" VARCHAR(64) PRIMARY KEY,
    "data" jsonb NOT NULL
);

CREATE TABLE "node_casper_contract_packages"
(
    "hash"   VARCHAR(64) PRIMARY KEY,
    "deploy" VARCHAR(64),
    "from"   VARCHAR(68),
    "data"   jsonb NOT NULL
);

CREATE TABLE "node_casper_contracts"
(
    "hash"    VARCHAR(64) PRIMARY KEY,
    "package" VARCHAR(64) NOT NULL,
    "deploy"  VARCHAR(64),
    "from"    VARCHAR(68),
    "type"    VARCHAR     NOT NULL,
    "score"   FLOAT       NOT NULL,
    "data"    jsonb       NOT NULL
);

CREATE TABLE "node_casper_named_keys"
(
    "uref"          VARCHAR(77) PRIMARY KEY,
    "name"          VARCHAR NOT NULL,
    "is_purse"      BOOLEAN NOT NULL,
    "initial_value" jsonb
);

CREATE TABLE "node_casper_contracts_named_keys"
(
    contract_hash  VARCHAR(64) references node_casper_contracts (hash),
    named_key_uref VARCHAR(77) references node_casper_named_keys (uref),
    primary key (contract_hash, named_key_uref)
);


CREATE TABLE "node_casper_rewards"
(
    "block"                VARCHAR(64) NOT NULL,
    "era"                  BIGINT      NOT NULL,
    "delegator_public_key" VARCHAR(68),
    "validator_public_key" VARCHAR(68) NOT NULL,
    "amount"               VARCHAR     NOT NULL
);

CREATE TABLE "node_casper_bids"
(
    "public_key"      VARCHAR(68) NOT NULL PRIMARY KEY,
    "bonding_purse"   VARCHAR     NOT NULL,
    "staked_amount"   NUMERIC     NOT NULL,
    "delegation_rate" INT         NOT NULL,
    "inactive"        BOOL        NOT NULL
);

CREATE TABLE "node_casper_delegators"
(
    "public_key"    VARCHAR(68) NOT NULL,
    "delegatee"     VARCHAR(68) NOT NULL,
    "staked_amount" NUMERIC     NOT NULL,
    "bonding_purse" VARCHAR     NOT NULL
);

CREATE TABLE "node_casper_accounts"
(
    "account_hash" VARCHAR(64) NOT NULL PRIMARY KEY,
    "public_key"   VARCHAR(68) UNIQUE,
    "main_purse"   VARCHAR(73) NOT NULL UNIQUE
);

CREATE TABLE "node_casper_purses"
(
    "purse"   VARCHAR(73) NOT NULL PRIMARY KEY,
    "balance" NUMERIC
);

ALTER TABLE "node_casper_delegators"
    ADD CONSTRAINT uAuction UNIQUE (public_key, delegatee, bonding_purse);

ALTER TABLE "node_casper_rewards"
    ADD FOREIGN KEY ("block") REFERENCES "node_casper_blocks" ("hash");

ALTER TABLE "node_casper_rewards"
    ADD CONSTRAINT uReward UNIQUE (block, era, delegator_public_key, validator_public_key);

ALTER TABLE "node_casper_deploys"
    ADD FOREIGN KEY ("block") REFERENCES "node_casper_blocks" ("hash");

ALTER TABLE "node_casper_blocks"
    ADD FOREIGN KEY ("hash") REFERENCES "node_casper_raw_blocks" ("hash");

ALTER TABLE "node_casper_deploys"
    ADD FOREIGN KEY ("hash") REFERENCES "node_casper_raw_deploys" ("hash");

ALTER TABLE "node_casper_contracts"
    ADD FOREIGN KEY ("package") REFERENCES "node_casper_contract_packages" ("hash");

ALTER TABLE "node_casper_contracts"
    ADD FOREIGN KEY ("deploy") REFERENCES "node_casper_deploys" ("hash");

ALTER TABLE "node_casper_contract_packages"
    ADD FOREIGN KEY ("deploy") REFERENCES "node_casper_deploys" ("hash");

CREATE INDEX ON "node_casper_deploys" ("block");
CREATE INDEX ON "node_casper_deploys" ("from");
CREATE INDEX ON "node_casper_deploys" ("contract_hash");
CREATE INDEX ON "node_casper_deploys" ("result");
CREATE INDEX ON "node_casper_deploys" ("timestamp");
CREATE INDEX ON "node_casper_delegators" ("delegatee");

CREATE VIEW node_casper_full_stats AS
SELECT count(*), type, date_trunc('day', timestamp) as day
from node_casper_deploys
WHERE timestamp >= NOW() - INTERVAL '14 DAY'
GROUP BY day, type;

CREATE VIEW node_casper_simple_stats AS
SELECT count(*), date_trunc('day', timestamp) as day
from node_casper_deploys
WHERE timestamp >= NOW() - INTERVAL '14 DAY'
GROUP BY day;

CREATE VIEW node_casper_total_rewards AS
SELECT sum(amount::NUMERIC) as total_rewards
FROM node_casper_rewards;

CREATE VIEW node_casper_total_staking AS
SELECT node_casper_delegators.public_key,
       sum(node_casper_delegators.staked_amount) AS sum
FROM node_casper_delegators
GROUP BY node_casper_delegators.public_key;

CREATE VIEW node_casper_stakers AS
WITH publicKeys as (SELECT DISTINCT public_key
                    FROM node_casper_delegators)
SELECT COUNT(*)
from publicKeys;

CREATE VIEW node_casper_mouvements AS
SELECT 'delegate'                                                as type,
       FLOOR(SUM((metadata ->> 'amount')::numeric) / 1000000000) as count,
       date_trunc('day', timestamp)                              as day
from node_casper_deploys
WHERE timestamp >= NOW() - INTERVAL '14 DAY'
  and metadata_type = 'delegate'
  AND result is true
GROUP BY day
UNION
SELECT 'undelegate'                                              as type,
       FLOOR(SUM((metadata ->> 'amount')::numeric) / 1000000000) as count,
       date_trunc('day', timestamp)                              as day
from node_casper_deploys
WHERE timestamp >= NOW() - INTERVAL '14 DAY'
  and metadata_type = 'undelegate'
  AND result is true
GROUP BY day
UNION
SELECT 'transfer'                                                as type,
       FLOOR(SUM((metadata ->> 'amount')::numeric) / 1000000000) as count,
       date_trunc('day', timestamp)                              as day
from node_casper_deploys
WHERE timestamp >= NOW() - INTERVAL '14 DAY'
  and type = 'transfer'
  AND result is true
GROUP BY day;

CREATE VIEW node_casper_rich_list AS
WITH node_casper_total_staking as (SELECT public_key, SUM(staked_amount) as total from node_casper_delegators group by public_key)
SELECT node_casper_accounts.public_key,
       node_casper_accounts.account_hash,
       coalesce(purse, node_casper_bids.bonding_purse)                                                         as purse,
       (COALESCE(balance, 0) + COALESCE(node_casper_bids.staked_amount, 0) + COALESCE(node_casper_total_staking.total, 0)) as total
from node_casper_purses
         FULL JOIN node_casper_accounts ON node_casper_purses.purse = node_casper_accounts.main_purse
         FULL JOIN node_casper_bids ON node_casper_accounts.public_key = node_casper_bids.public_key
         FULL JOIN node_casper_total_staking on node_casper_accounts.public_key = node_casper_total_staking.public_key
ORDER BY total desc;

CREATE VIEW node_casper_allowance AS
SELECT DISTINCT metadata -> 'spender' -> 'Hash' as spender, "from", contract_hash
FROM node_casper_deploys
where metadata_type = 'approve'
  and result = true
  and metadata -> 'spender' -> 'Hash' is not null
UNION
SELECT DISTINCT metadata -> 'spender' -> 'Account' as spender, "from", contract_hash
FROM node_casper_deploys
where metadata_type = 'approve'
  and result = true
  and metadata -> 'spender' -> 'Account' is not null;

CREATE VIEW node_casper_contracts_list AS
SELECT node_casper_contracts.hash as hash, package, node_casper_contracts.type as type, score, d.timestamp
from node_casper_contracts
         INNER JOIN node_casper_deploys d on node_casper_contracts.deploy = d.hash;

CREATE VIEW node_casper_auctions_list AS
SELECT node_casper_contracts.hash as hash, package, node_casper_contracts.type as type, score, d.timestamp
from node_casper_contracts
         INNER JOIN node_casper_deploys d on node_casper_contracts.deploy = d.hash
WHERE node_casper_contracts.hash in
      (SELECT contract_hash
       from node_casper_contracts_named_keys
       where named_key_uref in
             (SELECT uref
              from node_casper_named_keys
              where name = 'marketplace_account'
                and initial_value =
                    '"30f1d1b21e3a2c36b55fef940210edf43866f59038e22b24f867afd83e089da1"'));

CREATE FUNCTION node_casper_era_rewards(eraid integer) RETURNS NUMERIC AS
$$
SELECT sum(amount::NUMERIC)
FROM node_casper_rewards
where era = eraid;
$$ LANGUAGE SQL;

CREATE FUNCTION node_casper_total_validator_rewards(publickey VARCHAR(68), OUT node_casper_validator_rewards NUMERIC,
                                        OUT node_casper_total_rewards NUMERIC) AS
$$
SELECT sum(amount::NUMERIC)                 as total_rewards,
       (SELECT sum(amount::NUMERIC)
        FROM node_casper_rewards
        where validator_public_key = publickey
          and delegator_public_key is null) as validator_rewards
FROM node_casper_rewards
where validator_public_key = publickey;
$$ LANGUAGE SQL;

CREATE FUNCTION node_casper_total_account_rewards(publickey VARCHAR(68)) RETURNS NUMERIC AS
$$
SELECT sum(amount::NUMERIC)
FROM node_casper_rewards
where delegator_public_key = publickey;
$$ LANGUAGE SQL;

CREATE FUNCTION node_casper_block_details(blockhash VARCHAR(64), OUT total NUMERIC, OUT success NUMERIC, OUT failed NUMERIC,
                              OUT total_cost NUMERIC) AS
$$
SELECT count(*)                                                                   as total,
       (SELECT count(*) from node_casper_deploys where block = blockhash and result is true)  as success,
       (SELECT count(*) from node_casper_deploys where block = blockhash and result is false) as failed,
       sum(cost::NUMERIC)                                                         as total_cost
FROM node_casper_deploys
where block = blockhash;
$$ LANGUAGE SQL;

CREATE FUNCTION node_casper_contract_details(contracthash VARCHAR(64), OUT total NUMERIC, OUT success NUMERIC, OUT failed NUMERIC,
                                 OUT total_cost NUMERIC) AS
$$
SELECT count(*)                                                                              as total,
       (SELECT count(*) from node_casper_deploys where contract_hash = contracthash and result is true)  as success,
       (SELECT count(*) from node_casper_deploys where contract_hash = contracthash and result is false) as failed,
       sum(cost::NUMERIC)                                                                    as total_cost
FROM node_casper_deploys
where contract_hash = contracthash;
$$ LANGUAGE SQL;

CREATE FUNCTION node_casper_account_ercs20(publickey VARCHAR, accounthash VARCHAR)
    RETURNS TABLE
            (
                contract_hash VARCHAR(64)
            )
AS
$$
SELECT DISTINCT contract_hash
FROM node_casper_deploys
WHERE contract_hash IN (SELECT hash from node_casper_contracts where node_casper_contracts.type = 'erc20' or node_casper_contracts.type = 'uniswaperc20')
  and "from" = publickey
  and result is true
UNION
SELECT DISTINCT contract_hash
FROM node_casper_deploys
WHERE contract_hash IN (SELECT hash from node_casper_contracts where node_casper_contracts.type = 'erc20' or node_casper_contracts.type = 'uniswaperc20')
  and (metadata -> 'recipient' ->> 'Account' = accounthash
    or metadata ->> 'recipient' = accounthash)
  and result is true;
$$ LANGUAGE SQL;

CREATE FUNCTION node_casper_erc20_holders(contracthash VARCHAR)
    RETURNS TABLE
            (
                account VARCHAR
            )
AS
$$
SELECT DISTINCT "from" as account
FROM node_casper_deploys
WHERE contract_hash = contracthash
  and result is true
UNION
SELECT DISTINCT metadata -> 'recipient' ->> 'Account' as account
FROM node_casper_deploys
WHERE contract_hash = contracthash
  and metadata -> 'recipient' ->> 'Account' != ''
  and result is true
UNION
SELECT DISTINCT metadata ->> 'recipient' as account
FROM node_casper_deploys
WHERE contract_hash = contracthash
  and length(metadata ->> 'recipient') = 64
  and result is true;
$$ LANGUAGE SQL;

--DROP ROLE IF EXISTS web_anon;
--CREATE ROLE web_anon NOLOGIN;

grant usage on schema public to web_anon;
grant select on public.node_casper_blocks to web_anon;
grant select on public.node_casper_raw_blocks to web_anon;
grant select on public.node_casper_deploys to web_anon;
grant select on public.node_casper_raw_deploys to web_anon;
grant select on public.node_casper_contract_packages to web_anon;
grant select on public.node_casper_contracts to web_anon;
grant select on public.node_casper_named_keys to web_anon;
grant select on public.node_casper_contracts_named_keys to web_anon;
grant select on public.node_casper_rewards to web_anon;
grant select on public.node_casper_bids to web_anon;
grant select on public.node_casper_delegators to web_anon;
grant select on public.node_casper_accounts to web_anon;
grant select on public.node_casper_purses to web_anon;
grant select on public.node_casper_full_stats to web_anon;
grant select on public.node_casper_simple_stats to web_anon;
grant select on public.node_casper_total_rewards to web_anon;
grant select on public.node_casper_total_staking to web_anon;
grant select on public.node_casper_stakers to web_anon;
grant select on public.node_casper_mouvements to web_anon;
grant select on public.node_casper_rich_list to web_anon;
grant select on public.node_casper_contracts_list to web_anon;
grant select on public.node_casper_auctions_list to web_anon;
grant select on public.node_casper_allowance to web_anon;
grant execute on function node_casper_era_rewards(integer) to web_anon;
grant execute on function node_casper_total_validator_rewards(VARCHAR(68)) to web_anon;
grant execute on function node_casper_total_account_rewards(VARCHAR(68)) to web_anon;
grant execute on function node_casper_block_details(VARCHAR(64)) to web_anon;
grant execute on function node_casper_contract_details(VARCHAR(64)) to web_anon;
grant execute on function node_casper_account_ercs20(VARCHAR, VARCHAR) to web_anon;
grant execute on function node_casper_erc20_holders(VARCHAR) to web_anon;
