ALTER DATABASE [$(DatabaseName)]
    ADD LOG FILE (NAME = [x360ce_log], FILENAME = 'D:\SQLLOGS\x360ce_log.ldf', SIZE = 1024 KB, MAXSIZE = 2097152 MB, FILEGROWTH = 10 %);



