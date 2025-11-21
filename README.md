# Fetcher

Console app for retrieving azure blob files with ability to resume interrupted downloads

## Usage

```text
fetcher.exe --url <url> [--path <path> --threads <num> --key <key> --chunksize <megabytes> --debug]

--url:          Url of Azure blob file. Required.
--path:         Local path to save downloaded file. Default is current folder.
--threads:      Maximum number of parallel download threads. Default is 32 * number of processors.
--key:          SAS key for authentication. Will attempt to use current user credentials if omitted.
--chunksize:    File will be downloaded to separate chunk files, 1 per thread, and reassembled. Default is 256 MB.
--debug:        Writes debug information during download process if included.
```
