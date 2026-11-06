-- The database schema the tests run against: bud-tack-db after every migration
-- up to Transactions006_CreateBudgets (202611061710). Generated, never edited by hand:
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
CREATE TABLE transactions.budgets (
    id uuid NOT NULL,
    owned_by uuid NOT NULL,
    category_id uuid NOT NULL,
    amount numeric(14,2) NOT NULL,
    period_start date NOT NULL,
    period_end date NOT NULL,
    created_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    updated_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    created_by uuid NOT NULL,
    updated_by uuid NOT NULL,
    CONSTRAINT ck_budgets_amount_positive CHECK ((amount > (0)::numeric)),
    CONSTRAINT ck_budgets_period_ends_after_start CHECK ((period_end > period_start))
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
CREATE TABLE transactions.transaction_details (
    id uuid NOT NULL,
    transaction_id uuid NOT NULL,
    subcategory_id uuid NOT NULL,
    amount numeric(14,2) NOT NULL,
    created_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    updated_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    created_by uuid NOT NULL,
    updated_by uuid NOT NULL,
    line_number smallint NOT NULL,
    CONSTRAINT ck_transaction_details_amount_positive CHECK ((amount > (0)::numeric)),
    CONSTRAINT ck_transaction_details_line_number_positive CHECK ((line_number > 0))
);
CREATE TABLE transactions.transactions (
    id uuid NOT NULL,
    owned_by uuid NOT NULL,
    transaction_date date NOT NULL,
    description character varying(200) NOT NULL,
    created_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    updated_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    created_by uuid NOT NULL,
    updated_by uuid NOT NULL,
    deleted_at timestamp with time zone,
    deleted_by uuid,
    CONSTRAINT ck_transactions_deleted_together CHECK (((deleted_at IS NULL) = (deleted_by IS NULL))),
    CONSTRAINT ck_transactions_description_not_blank CHECK ((btrim((description)::text) <> ''::text))
);
ALTER TABLE ONLY identity.users
    ADD CONSTRAINT pk_users PRIMARY KEY (id);
ALTER TABLE ONLY transactions.budgets
    ADD CONSTRAINT pk_budgets PRIMARY KEY (id);
ALTER TABLE ONLY transactions.categories
    ADD CONSTRAINT pk_categories PRIMARY KEY (id);
ALTER TABLE ONLY transactions.subcategories
    ADD CONSTRAINT pk_subcategories PRIMARY KEY (id);
ALTER TABLE ONLY transactions.transaction_details
    ADD CONSTRAINT pk_transaction_details PRIMARY KEY (id);
ALTER TABLE ONLY transactions.transactions
    ADD CONSTRAINT pk_transactions PRIMARY KEY (id);
CREATE UNIQUE INDEX idx_users_email ON identity.users USING btree (email);
CREATE INDEX idx_budgets_category_id ON transactions.budgets USING btree (category_id);
CREATE INDEX idx_budgets_owned_by_period_start ON transactions.budgets USING btree (owned_by, period_start DESC);
CREATE UNIQUE INDEX idx_categories_owned_by_type_name ON transactions.categories USING btree (owned_by, type, lower((name)::text));
CREATE UNIQUE INDEX idx_subcategories_category_id_name ON transactions.subcategories USING btree (category_id, lower((name)::text));
CREATE INDEX idx_transaction_details_subcategory_id ON transactions.transaction_details USING btree (subcategory_id);
CREATE UNIQUE INDEX idx_transaction_details_transaction_id_line_number ON transactions.transaction_details USING btree (transaction_id, line_number);
CREATE INDEX idx_transactions_owned_by_transaction_date ON transactions.transactions USING btree (owned_by, transaction_date DESC, created_at DESC) WHERE (deleted_at IS NULL);
ALTER TABLE ONLY transactions.budgets
    ADD CONSTRAINT fk_budgets_category_id FOREIGN KEY (category_id) REFERENCES transactions.categories(id) ON DELETE RESTRICT;
ALTER TABLE ONLY transactions.subcategories
    ADD CONSTRAINT fk_subcategories_category_id FOREIGN KEY (category_id) REFERENCES transactions.categories(id) ON DELETE RESTRICT;
ALTER TABLE ONLY transactions.transaction_details
    ADD CONSTRAINT fk_transaction_details_subcategory_id FOREIGN KEY (subcategory_id) REFERENCES transactions.subcategories(id) ON DELETE RESTRICT;
ALTER TABLE ONLY transactions.transaction_details
    ADD CONSTRAINT fk_transaction_details_transaction_id FOREIGN KEY (transaction_id) REFERENCES transactions.transactions(id) ON DELETE RESTRICT;
