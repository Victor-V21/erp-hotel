\set ON_ERROR_STOP on

SELECT current_database() = :'database_name' AS connected_to_expected_database
\gset
\if :connected_to_expected_database
\else
    \echo 'La conexión no apunta a la base indicada.'
    \quit 3
\endif

REVOKE CREATE ON SCHEMA public FROM PUBLIC;
GRANT USAGE ON SCHEMA public TO :"application_role";
ALTER SCHEMA public OWNER TO :"migrator_role";

REVOKE ALL PRIVILEGES ON ALL TABLES IN SCHEMA public FROM :"application_role";
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO :"application_role";

REVOKE UPDATE, DELETE, TRUNCATE, REFERENCES, TRIGGER
    ON TABLE "AuditLogs" FROM :"application_role";
GRANT SELECT, INSERT ON TABLE "AuditLogs" TO :"application_role";

REVOKE ALL PRIVILEGES ON TABLE "__EFMigrationsHistory" FROM :"application_role";
GRANT SELECT ON TABLE "__EFMigrationsHistory" TO :"application_role";

REVOKE ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public FROM :"application_role";
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO :"application_role";

ALTER DEFAULT PRIVILEGES FOR ROLE :"migrator_role" IN SCHEMA public
    GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO :"application_role";
ALTER DEFAULT PRIVILEGES FOR ROLE :"migrator_role" IN SCHEMA public
    GRANT USAGE, SELECT ON SEQUENCES TO :"application_role";

SELECT format('REVOKE ALL PRIVILEGES ON DATABASE %I FROM PUBLIC', :'database_name')
\gexec
SELECT format('GRANT CONNECT ON DATABASE %I TO %I', :'database_name', :'migrator_role')
\gexec
SELECT format('GRANT CONNECT ON DATABASE %I TO %I', :'database_name', :'application_role')
\gexec
