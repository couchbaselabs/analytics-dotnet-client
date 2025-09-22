# Couchbase Analytics Performer

This is a gRPC server intended for internal testing purposes, which translates gRPC requests into Couchbase Analytics operations.
It is not intended for external use, and is not supported by Couchbase.

Proto definitions can be updated by running the `UpdateProtoFiles.sh` script sparsely checks out the proto files from `transactions-fit-performer` and copies them into this project, replacing existing ones.

Instructions:
1. If necessary, give yourself permissions to execute the script: `chmod +x UpdateProtoFiles.sh`
2. Make sure your machine is authenticated with GitHub and has access to the `transactions-fit-performer` repo.
3. Run the script `./UpdateProtoFiles.sh`
4. Rebuild the project to generate the C# files from the proto definitions.