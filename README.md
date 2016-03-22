# b2.net
.Net API and tools for the backblaze b2 cloud storage service

The following projects make up this repository:

* b2.core - C# interface to the b2 service
* b2.console - Command line program to upload files

## b2.console usage:
First, register the b2 account ID and application key:

    b2 auth --account acccount_id --appkey application_key

Then upload a single file:

    b2 upload --bucket bucket_name documents/file.txt documents/file.txt

Or a whole directory tree:

    b2 upload --bucket bucket_name documents /
