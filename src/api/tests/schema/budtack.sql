-- The database schema the tests run against: bud-tack-db after every migration
-- up to Transactions002_CreateSubcategories (202611060036). Generated, never edited by hand:
--   docker exec bud-tack-postgres-1 pg_dump --schema-only --no-owner --no-privileges \n--     --no-comments --schema=identity --schema=transactions -U postgres app_dev
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
CREATE TABLE transactions.categories (
    id uuid NOT NULL,
    owned_by uuid NOT NULL,
    name character varying(100) NOT NULL,
    type character varying(10) NOT NULL,
    is_active boolean DEFAULT true NOT NULL,
    created_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    updated_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    created_by uuid NOT NULL,
    updated_by uuid NOT NULL,
    CONSTRAINT ck_categories_name_not_blank CHECK ((btrim((name)::text) <> ''::text)),
    CONSTRAINT ck_categories_type CHECK (((type)::text = ANY ((ARRAY['income'::character varying, 'expense'::character varying])::text[])))
);
CREATE TABLE transactions.subcategories (
    id uuid NOT NULL,
    category_id uuid NOT NULL,
    name character varying(100) NOT NULL,
    is_active boolean DEFAULT true NOT NULL,
    created_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    updated_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    created_by uuid NOT NULL,
    updated_by uuid NOT NULL,
    CONSTRAINT ck_subcategories_name_not_blank CHECK ((btrim((name)::text) <> ''::text))
);
ALTER TABLE ONLY identity.users
    ADD CONSTRAINT pk_users PRIMARY KEY (id);
ALTER TABLE ONLY transactions.categories
    ADD CONSTRAINT pk_categories PRIMARY KEY (id);
ALTER TABLE ONLY transactions.subcategories
    ADD CONSTRAINT pk_subcategories PRIMARY KEY (id);
CREATE UNIQUE INDEX idx_users_email ON identity.users USING btree (email);
CREATE UNIQUE INDEX idx_categories_owned_by_type_name ON transactions.categories USING btree (owned_by, type, lower((name)::text));
CREATE UNIQUE INDEX idx_subcategories_category_id_name ON transactions.subcategories USING btree (category_id, lower((name)::text));
ALTER TABLE ONLY transactions.subcategories
    ADD CONSTRAINT fk_subcategories_category_id FOREIGN KEY (category_id) REFERENCES transactions.categories(id) ON DELETE RESTRICT;
