\set ON_ERROR_STOP on
\getenv migrator_password HOTEL_ERP_MIGRATOR_PASSWORD
\getenv application_password HOTEL_ERP_APPLICATION_PASSWORD

SELECT format(
    'CREATE ROLE %I LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT NOREPLICATION NOBYPASSRLS PASSWORD %L',
    :'migrator_role',
    :'migrator_password')
WHERE NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = :'migrator_role')
\gexec

SELECT format(
    'ALTER ROLE %I WITH LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT NOREPLICATION NOBYPASSRLS PASSWORD %L',
    :'migrator_role',
    :'migrator_password')
\gexec

SELECT format(
    'CREATE ROLE %I LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT NOREPLICATION NOBYPASSRLS PASSWORD %L',
    :'application_role',
    :'application_password')
WHERE NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = :'application_role')
\gexec

SELECT format(
    'ALTER ROLE %I WITH LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT NOREPLICATION NOBYPASSRLS PASSWORD %L',
    :'application_role',
    :'application_password')
\gexec

SELECT format('CREATE DATABASE %I OWNER %I', :'database_name', :'migrator_role')
WHERE NOT EXISTS (SELECT 1 FROM pg_database WHERE datname = :'database_name')
\gexec

SELECT format('ALTER DATABASE %I OWNER TO %I', :'database_name', :'migrator_role')
\gexec
SELECT format('REVOKE ALL PRIVILEGES ON DATABASE %I FROM PUBLIC', :'database_name')
\gexec
SELECT format('GRANT CONNECT ON DATABASE %I TO %I', :'database_name', :'migrator_role')
\gexec
SELECT format('GRANT CONNECT ON DATABASE %I TO %I', :'database_name', :'application_role')
\gexec
