#!/bin/bash
set -a
source .env 
set a+ 

dotnet tool run dotnet-ef dbcontext scaffold \
"$CONN_STR" \
Npgsql.EntityFrameworkCore.PostgreSQL \
 --context ApplicationDbContext --context-dir . \
 --output-dir Entities \
 --namespace JerneIF25.DataAccess.Entities \
 --use-database-names --no-onconfiguring --force