-- The database schema the tests run against: bud-tack-db after every migration
-- up to Identity001_CreateUsers (202611052230). Generated, never edited by hand:
--   docker exec bud-tack-postgres-1 pg_dump --schema-only --no-owner --no-privileges \
--     --no-comments --schema=identity --schema=transactions -U postgres app_dev
-- Regenerate it in the pull request that needs a newer migration.

CREATE SCHEMA identity;
CREATE SCHEMA transactions;
CREATE TABLE identity.users (
    id uuid NOT NULL,
    name character varying(100) NOT NULL,
    email character varying(254) NOT NULL,
    password_hash character varying(255) NOT NULL,
    created_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    updated_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    created_by uuid,
    updated_by uuid,
    CONSTRAINT ck_users_email_lowercase CHECK (((email)::text = lower((email)::text))),
    CONSTRAINT ck_users_name_not_blank CHECK ((btrim((name)::text) <> ''::text))
);
ALTER TABLE ONLY identity.users
    ADD CONSTRAINT pk_users PRIMARY KEY (id);
CREATE UNIQUE INDEX idx_users_email ON identity.users USING btree (email);
