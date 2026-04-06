-- Initialize PhotoApp Databases
-- This script is run when PostgreSQL container starts

-- AuthService Database
CREATE DATABASE authservice
    WITH
    ENCODING = 'UTF8'
    LC_COLLATE = 'en_US.UTF-8'
    LC_CTYPE = 'en_US.UTF-8';

-- UserManagementService Database
CREATE DATABASE usermanagementservice
    WITH
    ENCODING = 'UTF8'
    LC_COLLATE = 'en_US.UTF-8'
    LC_CTYPE = 'en_US.UTF-8';

-- MediaService Database
CREATE DATABASE mediaservice
    WITH
    ENCODING = 'UTF8'
    LC_COLLATE = 'en_US.UTF-8'
    LC_CTYPE = 'en_US.UTF-8';

-- AuditService Database
CREATE DATABASE auditservice
    WITH
    ENCODING = 'UTF8'
    LC_COLLATE = 'en_US.UTF-8'
    LC_CTYPE = 'en_US.UTF-8';

-- Grant privileges to photoapp_user on all databases
GRANT ALL PRIVILEGES ON DATABASE authservice TO photoapp_user;
GRANT ALL PRIVILEGES ON DATABASE usermanagementservice TO photoapp_user;
GRANT ALL PRIVILEGES ON DATABASE mediaservice TO photoapp_user;
GRANT ALL PRIVILEGES ON DATABASE auditservice TO photoapp_user;

-- Create schema for each service in photoapp database (for shared tables)
\c photoapp
CREATE SCHEMA IF NOT EXISTS audit;
GRANT ALL PRIVILEGES ON SCHEMA audit TO photoapp_user;

-- Create extensions for PostgreSQL
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pg_stat_statements";
