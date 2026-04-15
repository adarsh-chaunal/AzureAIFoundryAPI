-- Run once, connected to the `master` database, if the clinical catalog does not exist yet.
IF DB_ID(N'EhrClinical') IS NULL
BEGIN
    CREATE DATABASE EhrClinical;
END;
