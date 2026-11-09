-- The database schema the tests run against: bud-tack-db after every migration
-- up to Transactions008_AddDeletionReason (202611092102). Generated, never edited by hand:
--   docker exec bud-tack-postgres-1 pg_dump --schema-only --no-owner --no-privileges \n--     --no-comments --schema=identity --schema=transactions -U postgres app_dev
-- Regenerate it in the pull request that needs a newer migration.

--
-- PostgreSQL database dump
--
\restrict xeDHhZpdmdHV9y19wbGZTM5QgYLPDG9nac5Ai11Q6wiqfBACMSmYi4M6ciWtrYc
-- Dumped from database version 17.11
-- Dumped by pg_dump version 17.11
SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET transaction_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;
--
-- Name: identity; Type: SCHEMA; Schema: -; Owner: -
--
CREATE SCHEMA identity;
--
-- Name: transactions; Type: SCHEMA; Schema: -; Owner: -
--
CREATE SCHEMA transactions;
SET default_tablespace = '';
SET default_table_access_method = heap;
--
-- Name: account_tokens; Type: TABLE; Schema: identity; Owner: -
--
CREATE TABLE identity.account_tokens (
    id uuid NOT NULL,
    user_id uuid NOT NULL,
    purpose character varying(20) NOT NULL,
    token_hash character varying(64) NOT NULL,
    expires_at timestamp with time zone NOT NULL,
    used_at timestamp with time zone,
    created_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    created_by uuid NOT NULL,
    CONSTRAINT ck_account_tokens_purpose CHECK (((purpose)::text = ANY ((ARRAY['ACTIVATION'::character varying, 'PASSWORD_RESET'::character varying])::text[])))
);
--
-- Name: users; Type: TABLE; Schema: identity; Owner: -
--
CREATE TABLE identity.users (
    id uuid NOT NULL,
    name character varying(100) NOT NULL,
    email character varying(254) NOT NULL,
    password_hash character varying(255),
    created_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    updated_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    created_by uuid,
    updated_by uuid,
    role character varying(10) DEFAULT 'USER'::character varying NOT NULL,
    CONSTRAINT ck_users_email_lowercase CHECK (((email)::text = lower((email)::text))),
    CONSTRAINT ck_users_name_not_blank CHECK ((btrim((name)::text) <> ''::text)),
    CONSTRAINT ck_users_role CHECK (((role)::text = ANY ((ARRAY['USER'::character varying, 'ADMIN'::character varying])::text[])))
);
--
-- Name: budgets; Type: TABLE; Schema: transactions; Owner: -
--
CREATE TABLE transactions.budgets (
    id uuid NOT NULL,
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
--
-- Name: categories; Type: TABLE; Schema: transactions; Owner: -
--
CREATE TABLE transactions.categories (
    id uuid NOT NULL,
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
--
-- Name: subcategories; Type: TABLE; Schema: transactions; Owner: -
--
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
--
-- Name: transaction_details; Type: TABLE; Schema: transactions; Owner: -
--
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
--
-- Name: transactions; Type: TABLE; Schema: transactions; Owner: -
--
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
    deletion_reason character varying(200),
    CONSTRAINT ck_transactions_deleted_together CHECK (((deleted_at IS NULL) = (deleted_by IS NULL))),
    CONSTRAINT ck_transactions_deletion_reason CHECK ((((deleted_at IS NULL) AND (deletion_reason IS NULL)) OR ((deleted_at IS NOT NULL) AND (deletion_reason IS NOT NULL) AND (length(btrim((deletion_reason)::text)) >= 3)))),
    CONSTRAINT ck_transactions_description_not_blank CHECK ((btrim((description)::text) <> ''::text))
);
--
-- Name: account_tokens pk_account_tokens; Type: CONSTRAINT; Schema: identity; Owner: -
--
ALTER TABLE ONLY identity.account_tokens
    ADD CONSTRAINT pk_account_tokens PRIMARY KEY (id);
--
-- Name: users pk_users; Type: CONSTRAINT; Schema: identity; Owner: -
--
ALTER TABLE ONLY identity.users
    ADD CONSTRAINT pk_users PRIMARY KEY (id);
--
-- Name: budgets pk_budgets; Type: CONSTRAINT; Schema: transactions; Owner: -
--
ALTER TABLE ONLY transactions.budgets
    ADD CONSTRAINT pk_budgets PRIMARY KEY (id);
--
-- Name: categories pk_categories; Type: CONSTRAINT; Schema: transactions; Owner: -
--
ALTER TABLE ONLY transactions.categories
    ADD CONSTRAINT pk_categories PRIMARY KEY (id);
--
-- Name: subcategories pk_subcategories; Type: CONSTRAINT; Schema: transactions; Owner: -
--
ALTER TABLE ONLY transactions.subcategories
    ADD CONSTRAINT pk_subcategories PRIMARY KEY (id);
--
-- Name: transaction_details pk_transaction_details; Type: CONSTRAINT; Schema: transactions; Owner: -
--
ALTER TABLE ONLY transactions.transaction_details
    ADD CONSTRAINT pk_transaction_details PRIMARY KEY (id);
--
-- Name: transactions pk_transactions; Type: CONSTRAINT; Schema: transactions; Owner: -
--
ALTER TABLE ONLY transactions.transactions
    ADD CONSTRAINT pk_transactions PRIMARY KEY (id);
--
-- Name: idx_account_tokens_token_hash; Type: INDEX; Schema: identity; Owner: -
--
CREATE UNIQUE INDEX idx_account_tokens_token_hash ON identity.account_tokens USING btree (token_hash);
--
-- Name: idx_account_tokens_user_id; Type: INDEX; Schema: identity; Owner: -
--
CREATE INDEX idx_account_tokens_user_id ON identity.account_tokens USING btree (user_id);
--
-- Name: idx_users_email; Type: INDEX; Schema: identity; Owner: -
--
CREATE UNIQUE INDEX idx_users_email ON identity.users USING btree (email);
--
-- Name: idx_budgets_category_id; Type: INDEX; Schema: transactions; Owner: -
--
CREATE INDEX idx_budgets_category_id ON transactions.budgets USING btree (category_id);
--
-- Name: idx_budgets_period_start; Type: INDEX; Schema: transactions; Owner: -
--
CREATE INDEX idx_budgets_period_start ON transactions.budgets USING btree (period_start DESC);
--
-- Name: idx_categories_type_name; Type: INDEX; Schema: transactions; Owner: -
--
CREATE UNIQUE INDEX idx_categories_type_name ON transactions.categories USING btree (type, lower((name)::text));
--
-- Name: idx_subcategories_category_id_name; Type: INDEX; Schema: transactions; Owner: -
--
CREATE UNIQUE INDEX idx_subcategories_category_id_name ON transactions.subcategories USING btree (category_id, lower((name)::text));
--
-- Name: idx_transaction_details_subcategory_id; Type: INDEX; Schema: transactions; Owner: -
--
CREATE INDEX idx_transaction_details_subcategory_id ON transactions.transaction_details USING btree (subcategory_id);
--
-- Name: idx_transaction_details_transaction_id_line_number; Type: INDEX; Schema: transactions; Owner: -
--
CREATE UNIQUE INDEX idx_transaction_details_transaction_id_line_number ON transactions.transaction_details USING btree (transaction_id, line_number);
--
-- Name: idx_transactions_owned_by_transaction_date; Type: INDEX; Schema: transactions; Owner: -
--
CREATE INDEX idx_transactions_owned_by_transaction_date ON transactions.transactions USING btree (owned_by, transaction_date DESC, created_at DESC) WHERE (deleted_at IS NULL);
--
-- Name: account_tokens fk_account_tokens_user_id; Type: FK CONSTRAINT; Schema: identity; Owner: -
--
ALTER TABLE ONLY identity.account_tokens
    ADD CONSTRAINT fk_account_tokens_user_id FOREIGN KEY (user_id) REFERENCES identity.users(id) ON DELETE RESTRICT;
--
-- Name: budgets fk_budgets_category_id; Type: FK CONSTRAINT; Schema: transactions; Owner: -
--
ALTER TABLE ONLY transactions.budgets
    ADD CONSTRAINT fk_budgets_category_id FOREIGN KEY (category_id) REFERENCES transactions.categories(id) ON DELETE RESTRICT;
--
-- Name: subcategories fk_subcategories_category_id; Type: FK CONSTRAINT; Schema: transactions; Owner: -
--
ALTER TABLE ONLY transactions.subcategories
    ADD CONSTRAINT fk_subcategories_category_id FOREIGN KEY (category_id) REFERENCES transactions.categories(id) ON DELETE RESTRICT;
--
-- Name: transaction_details fk_transaction_details_subcategory_id; Type: FK CONSTRAINT; Schema: transactions; Owner: -
--
ALTER TABLE ONLY transactions.transaction_details
    ADD CONSTRAINT fk_transaction_details_subcategory_id FOREIGN KEY (subcategory_id) REFERENCES transactions.subcategories(id) ON DELETE RESTRICT;
--
-- Name: transaction_details fk_transaction_details_transaction_id; Type: FK CONSTRAINT; Schema: transactions; Owner: -
--
ALTER TABLE ONLY transactions.transaction_details
    ADD CONSTRAINT fk_transaction_details_transaction_id FOREIGN KEY (transaction_id) REFERENCES transactions.transactions(id) ON DELETE RESTRICT;
--
-- PostgreSQL database dump complete
--
\unrestrict xeDHhZpdmdHV9y19wbGZTM5QgYLPDG9nac5Ai11Q6wiqfBACMSmYi4M6ciWtrYc
