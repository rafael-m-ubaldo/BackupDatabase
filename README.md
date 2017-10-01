# BackupDatabase
by Rafael Ubaldo Sr
Command Line utility that creates a full backup of a Microsoft SQL database to a (.bak) backup file with an option to copy and date time stamp the backup.

Command line arguments: (case insensitive)
-s  The SQL Database Server computer/machine name 
-d  The database to be backed up.
-u  Username
-p  Password
Note: Currently,only SQL authentication is supported. (later version will have Windows Authentication)
-b  The full path of the backup file (e.g., C:\Backup Directory\DatabaseName.bak). This filespec is local to the SQL Database Server. If the backup file already exists when BackupDatabase is ran, this backup replaces the existing database file instead of appending to that file - this backup is a FULL database backup.
-o  This option is used with option -f below to reference the filespec in the above option -b. If SQL Server is remote (not on the same computer running BackupDatabase.exe), this needs to be the equivalent UNC filespec. (e.g., \\Computer\Share\DatabaseName.bak). 
-f  This option is the filespec of a copy of the database backup file.  The filename will be date stamped with the date when the backup was started. If this option is omitted, a copy of the backup is not made.
