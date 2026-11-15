# zlib, inflate only

The files here are zlib's own, unchanged, from its official repository: only those that inflating a gzip stream in
memory needs. The xinput library uses them to read its built-in controller databases from the gzip compressed files
the engine carries (`Native/x360ce/Sdl.cpp`). zlib's gzip file functions (`gzlib.c`, `gzread.c`, `gzwrite.c`) and
deflate are not taken; the files are compiled with `Z_SOLO`, so zlib allocates through the library's own functions.

- Repository: https://github.com/madler/zlib
- Release: zlib 1.4, tag `v1.4` (tag object `dd9d5c2d62b52e8acb0e678de5768087eedce914`), commit
  `99623ef7097352da7993ba614161fd6a311e046e`, tagged by Mark Adler on 2026-11-05.
- Archive: `zlib-1.4.tar.gz` from https://zlib.net/zlib-1.4.tar.gz, the same file as the release's
  https://github.com/madler/zlib/releases/download/v1.4/zlib-1.4.tar.gz, SHA-256
  `02dd159783677a749c04adbdd02b55b8a2bcc7e054f51b76b549199a916ad584`.

## How it was verified

1. The archive's SHA-256 equals the one zlib.net publishes for `zlib-1.4.tar.gz`, and the copy from the GitHub release
   is the same file.
2. Its signature, `zlib-1.4.tar.gz.asc`, is a good signature by Mark Adler's key
   `5ED4 6A67 21D3 6558 7791 E2AA 783F CD8E 58BC AFBA`. The key came from keys.openpgp.org and keyserver.ubuntu.com,
   which gave the same key; his GitHub account lists the same key ID, `783FCD8E58BCAFBA`.
3. Each file below, taken from the verified archive, has the git blob hash of the same file at the tag's commit, as
   the GitHub API gives the tree.

| File | SHA-256 | Git blob at v1.4 |
|---|---|---|
| adler32.c | 9cd1443a24ff2a3053961695bd432035c58347386a420d3388232376ebabe211 | 04b81d29bad1691878648d7ae1680726abd905e4 |
| crc32.c | dd941a0a499a27517e3be0624c2e0dc683f0591bf29dd1ed54e65443cb19beb6 | 32b1d0457886a674329769cb70f5597fd43422dc |
| crc32.h | 9a2223575183ac2ee8a247f20bf3ac066e8bd0140369556bdbdffc777435749e | 137df68d616c5ed38913579ba37b4e4c533fa903 |
| inffast.c | f96787a11bb7131ffd2221fb40db27f6a5a1dfb12f59c0bc388807010749c34a | d1657f3f30348aedf10add2838004a7ea0344c39 |
| inffast.h | 05cc5dc9ff1da7b8b52a4bd8bda0d8a5c236a2f39efe84b941516ea13857e6c5 | 49c6d156c5c652dbd9e2e6aff90e20eb279a2b37 |
| inffixed.h | fd7c4b1dbdacb283421d0675744aaecda44e840e183b5131448bac94564f6adb | 05ce88e4ff5cd4e7ffe05d3a8309af1bf3b0e989 |
| inflate.c | c91ab7595ba8ef11030af76a614fc3138d0ec4cf995f171a9f435e19b1e6d7bc | 1aed584538bf59a74533e3bf2ca0576a75371f17 |
| inflate.h | 7c58a1d5b5bd553493d58431c4dac031f99fb4b6c9890cc11ceab22d08e6bc2e | e7dfc8e62b55a22bd37c06734816a0210bcb8dde |
| inftrees.c | 7ab9c04acc2e9888c8c1fddefc730a850f69a3d80a7a702433a35383e36efa9c | 0fb8570b0ae3620d83a493a90201f5d4113fbc1c |
| inftrees.h | 97897f41e5d55b426b106c230111e99fec44cd5197a71e6a3750d2e7e17b16bc | 20eb2c03b8b456da9e8fa13d161136134ab87656 |
| zconf.h | 1e91041e94e3f13a12b770be849c4c674e37caddf94e37acddf853bb54a09adf | 3d308f6887fdb24a6a0e2e7a7a11b283fba4677a |
| zlib.h | 2c404dcaa08709c2a129aea18f3dad4fb5acc5cb52ef12b6223bf8e78c195801 | c7e50b834dbb03b8d30fe5dee3e5172e743ff8bd |
| zutil.c | c195fe0ff472545b231542ab6a633a97117710cea64f98c5d468775ed89eff55 | 6c01021dfe433c266fbc4ba7578c4df9d49ece01 |
| zutil.h | a12243cc6d338ffad0c8e3500fbc05bc7fdcdc25a469393b1c2f7e5df307fff3 | a9bc23ca62956e59b6ee304b0b6889f3ca776d7b |
| LICENSE | e32ff4e00d9d94930537635291da39e7e612703334bf6fde8c7f1686fe8a45a2 | b7a69d058e616651eae27b3f90c0b7fd36c099b2 |
| README | 5ba0fa00469b780503932c2967d34b857a15ba2354f4f86d6c747bc6646acb65 | 265c8b47d316c409ab970894c3c94f40796a2001 |

To take a newer release, repeat the three checks, replace the files and this record together, and build the library.
