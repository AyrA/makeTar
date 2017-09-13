# makeTar
Very primitive Utility to create TAR files. Never seeks output stream

Useful for delivering tar files from a Webserver without the need of an external utility or a temporary file.

If Wrapped inside the GZipStream (provided by .NET) it essentially creates .tar.gz archives.
