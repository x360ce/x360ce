/*  minIni - Multi-Platform INI file parser, suitable for embedded systems
 *
 *  These routines are in part based on the article "Multiplatform .INI Files"
 *  by Joseph J. Graf in the March 1994 issue of Dr. Dobb's Journal.
 *
 *  Copyright (c) CompuPhase, 2008-2011
 *
 *  Licensed under the Apache License, Version 2.0 (the "License"); you may not
 *  use this file except in compliance with the License. You may obtain a copy
 *  of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 *  Unless required by applicable law or agreed to in writing, software
 *  distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 *  WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
 *  License for the specific language governing permissions and limitations
 *  under the License.
 *
 *  Version: $Id: minIni.c 33 2011-06-20 10:51:17Z thiadmer.riemersma $
 */

#if (defined _UNICODE || defined __UNICODE__ || defined UNICODE) && !defined MININI_ANSI
# if !defined UNICODE   /* for Windows */
#   define UNICODE
# endif
# if !defined _UNICODE  /* for C library */
#   define _UNICODE
# endif
#endif

#define MININI_IMPLEMENTATION
#include "minIni.h"
#if defined NDEBUG
  #define assert(e)
#else
  #include <assert.h>
#endif

#if defined __linux || defined __linux__
  #define __LINUX__
#endif
#if defined FREEBSD && !defined __FreeBSD__
  #define __FreeBSD__
#endif
#if !defined strnicmp
  #if defined __LINUX__ || defined __FreeBSD__ || defined __OpenBSD__ || defined __APPLE__
    #define strnicmp  strncasecmp
  #endif
#endif

#if !defined INI_LINETERM
  #define INI_LINETERM    __T("\n")
#endif
#if !defined INI_FILETYPE
  #error Missing definition for INI_FILETYPE.
#endif

#if !defined sizearray
  #define sizearray(a)    (sizeof(a) / sizeof((a)[0]))
#endif

enum quote_option {
  QUOTE_NONE,
  QUOTE_ENQUOTE,
  QUOTE_DEQUOTE,
};

static TCHAR *skipleading(const TCHAR *str)
{
  assert(str != NULL);
  while (*str != '\0' && *str <= ' ')
    str++;
  return (TCHAR *)str;
}

static TCHAR *skiptrailing(const TCHAR *str, const TCHAR *base)
{
  assert(str != NULL);
  assert(base != NULL);
  while (str > base && *(str-1) <= ' ')
    str--;
  return (TCHAR *)str;
}

static TCHAR *striptrailing(TCHAR *str)
{
  TCHAR *ptr = skiptrailing(_tcschr(str, '\0'), str);
  assert(ptr != NULL);
  *ptr = '\0';
  return str;
}

static TCHAR *save_strncpy(TCHAR *dest, const TCHAR *source, size_t maxlen, enum quote_option option)
{
  size_t d, s;

  assert(maxlen>0);
  if (option == QUOTE_ENQUOTE && maxlen < 3)
    option = QUOTE_NONE;  /* cannot store two quotes and a terminating zero in less than 3 characters */

  switch (option) {
  case QUOTE_NONE:
    for (d = 0; d < maxlen - 1 && source[d] != '\0'; d++)
      dest[d] = source[d];
    assert(d < maxlen);
    dest[d] = '\0';
    break;
  case QUOTE_ENQUOTE:
    d = 0;
    dest[d++] = '"';
    for (s = 0; source[s] != '\0' && d < maxlen - 2; s++, d++) {
      if (source[s] == '"') {
        if (d >= maxlen - 3)
          break;  /* no space to store the escape character plus the one that follows it */
        dest[d++] = '\\';
      } /* if */
      dest[d] = source[s];
    } /* for */
    dest[d++] = '"';
    dest[d] = '\0';
    break;
  case QUOTE_DEQUOTE:
    for (d = s = 0; source[s] != '\0' && d < maxlen - 1; s++, d++) {
      if ((source[s] == '"' || source[s] == '\\') && source[s + 1] == '"')
        s++;
      dest[d] = source[s];
    } /* for */
    dest[d] = '\0';
    break;
  default:
    assert(0);
  } /* switch */

  return dest;
}

static int getkeystring(INI_FILETYPE *fp, const TCHAR *Section, const TCHAR *Key,
                        int idxSection, int idxKey, TCHAR *Buffer, int BufferSize)
{
  TCHAR *sp, *ep;
  int len, idx, isstring;
  enum quote_option quotes;
  TCHAR LocalBuffer[INI_BUFFERSIZE];

  assert(fp != NULL);
  /* Move through file 1 line at a time until a section is matched or EOF. If
   * parameter Section is NULL, only look at keys above the first section. If
   * idxSection is postive, copy the relevant section name.
   */
  len = (Section != NULL) ? _tcslen(Section) : 0;
  if (len > 0 || idxSection >= 0) {
    idx = -1;
    do {
      if (!ini_read(LocalBuffer, INI_BUFFERSIZE, fp))
        return 0;
      sp = skipleading(LocalBuffer);
      ep = _tcschr(sp, ']');
    } while (*sp != '[' || ep == NULL || (((int)(ep-sp-1) != len || _tcsnicmp(sp+1,Section,len) != 0) && ++idx != idxSection));
    if (idxSection >= 0) {
      if (idx == idxSection) {
        assert(ep != NULL);
        assert(*ep == ']');
        *ep = '\0';
        save_strncpy(Buffer, sp + 1, BufferSize, QUOTE_NONE);
        return 1;
      } /* if */
      return 0; /* no more section found */
    } /* if */
  } /* if */

  /* Now that the section has been found, find the entry.
   * Stop searching upon leaving the section's area.
   */
  assert(Key != NULL || idxKey >= 0);
  len = (Key != NULL) ? (int)_tcslen(Key) : 0;
  idx = -1;
  do {
    if (!ini_read(LocalBuffer,INI_BUFFERSIZE,fp) || *(sp = skipleading(LocalBuffer)) == '[')
      return 0;
    sp = skipleading(LocalBuffer);
    ep = _tcschr(sp, '='); /* Parse out the equal sign */
    if (ep == NULL)
      ep = _tcschr(sp, ':');
  } while (*sp == ';' || *sp == '#' || ep == NULL || (((int)(skiptrailing(ep,sp)-sp) != len || _tcsnicmp(sp,Key,len) != 0) && ++idx != idxKey));
  if (idxKey >= 0) {
    if (idx == idxKey) {
      assert(ep != NULL);
      assert(*ep == '=' || *ep == ':');
      *ep = '\0';
      striptrailing(sp);
      save_strncpy(Buffer, sp, BufferSize, QUOTE_NONE);
      return 1;
    } /* if */
    return 0;   /* no more key found (in this section) */
  } /* if */

  /* Copy up to BufferSize chars to buffer */
  assert(ep != NULL);
  assert(*ep == '=' || *ep == ':');
  sp = skipleading(ep + 1);
  /* Remove a trailing comment */
  isstring = 0;
  for (ep = sp; *ep != '\0' && ((*ep != ';' && *ep != '#') || isstring); ep++) {
    if (*ep == '"') {
      if (*(ep + 1) == '"')
        ep++;                 /* skip "" (both quotes) */
      else
        isstring = !isstring; /* single quote, toggle isstring */
    } else if (*ep == '\\' && *(ep + 1) == '"') {
      ep++;                   /* skip \" (both quotes */
    } /* if */
  } /* for */
  assert(ep != NULL && (*ep == '\0' || *ep == ';' || *ep == '#'));
  *ep = '\0';                 /* terminate at a comment */
  striptrailing(sp);
  /* Remove double quotes surrounding a value */
  quotes = QUOTE_NONE;
  if (*sp == '"' && (ep = _tcschr(sp, '\0')) != NULL && *(ep - 1) == '"') {
    sp++;
    *--ep = '\0';
    quotes = QUOTE_DEQUOTE;   /* this is a string, so remove escaped characters */
  } /* if */
  save_strncpy(Buffer, sp, BufferSize, quotes);
  return 1;
}

/** ini_gets()
 * \param Section     the name of the section to search for
 * \param Key         the name of the entry to find the value of
 * \param DefValue    default string in the event of a failed read
 * \param Buffer      a pointer to the buffer to copy into
 * \param BufferSize  the maximum number of characters to copy
 * \param Filename    the name and full path of the .ini file to read from
 *
 * \return            the number of characters copied into the supplied buffer
 */
int ini_gets(const TCHAR *Section, const TCHAR *Key, const TCHAR *DefValue,
             TCHAR *Buffer, int BufferSize, const TCHAR *Filename)
{
  INI_FILETYPE fp;
  int ok = 0;

  if (Buffer == NULL || BufferSize <= 0 || Key == NULL)
    return 0;
  if (ini_openread(Filename, &fp)) {
    ok = getkeystring(&fp, Section, Key, -1, -1, Buffer, BufferSize);
    (void)ini_close(&fp);
  } /* if */
  if (!ok)
    save_strncpy(Buffer, DefValue, BufferSize, QUOTE_NONE);
  return _tcslen(Buffer);
}

/** ini_getl()
 * \param Section     the name of the section to search for
 * \param Key         the name of the entry to find the value of
 * \param DefValue    the default value in the event of a failed read
 * \param Filename    the name of the .ini file to read from
 *
 * \return            the value located at Key
 */
long ini_getl(const TCHAR *Section, const TCHAR *Key, long DefValue, const TCHAR *Filename)
{
  TCHAR LocalBuffer[64];
  int len = ini_gets(Section, Key, __T(""), LocalBuffer, sizearray(LocalBuffer), Filename);
  return (len == 0) ? DefValue : _tcstol(LocalBuffer, NULL, 10);
}

#if defined INI_REAL
/** ini_getf()
 * \param Section     the name of the section to search for
 * \param Key         the name of the entry to find the value of
 * \param DefValue    the default value in the event of a failed read
 * \param Filename    the name of the .ini file to read from
 *
 * \return            the value located at Key
 */
INI_REAL ini_getf(const TCHAR *Section, const TCHAR *Key, INI_REAL DefValue, const TCHAR *Filename)
{
  TCHAR LocalBuffer[64];
  int len = ini_gets(Section, Key, __T(""), LocalBuffer, sizearray(LocalBuffer), Filename);
  return (len == 0) ? DefValue : ini_atof(LocalBuffer);
}
#endif

/** ini_getbool()
 * \param Section     the name of the section to search for
 * \param Key         the name of the entry to find the value of
 * \param DefValue    default value in the event of a failed read; it should
 *                    zero (0) or one (1).
 * \param Buffer      a pointer to the buffer to copy into
 * \param BufferSize  the maximum number of characters to copy
 * \param Filename    the name and full path of the .ini file to read from
 *
 * A true boolean is found if one of the following is matched:
 * - A string starting with 'y' or 'Y'
 * - A string starting with 't' or 'T'
 * - A string starting with '1'
 *
 * A false boolean is found if one of the following is matched:
 * - A string starting with 'n' or 'N'
 * - A string starting with 'f' or 'F'
 * - A string starting with '0'
 *
 * \return            the true/false flag as interpreted at Key
 */
int ini_getbool(const TCHAR *Section, const TCHAR *Key, int DefValue, const TCHAR *Filename)
{
  TCHAR LocalBuffer[2];
  int ret;

  ini_gets(Section, Key, __T(""), LocalBuffer, sizearray(LocalBuffer), Filename);
  LocalBuffer[0] = _totupper(LocalBuffer[0]);
  if (LocalBuffer[0] == 'Y' || LocalBuffer[0] == '1' || LocalBuffer[0] == 'T')
    ret = 1;
  else if (LocalBuffer[0] == 'N' || LocalBuffer[0] == '0' || LocalBuffer[0] == 'F')
    ret = 0;
  else
    ret = DefValue;

  return(ret);
}

/** ini_getsection()
 * \param idx         the zero-based sequence number of the section to return
 * \param Buffer      a pointer to the buffer to copy into
 * \param BufferSize  the maximum number of characters to copy
 * \param Filename    the name and full path of the .ini file to read from
 *
 * \return            the number of characters copied into the supplied buffer
 */
int  ini_getsection(int idx, TCHAR *Buffer, int BufferSize, const TCHAR *Filename)
{
  INI_FILETYPE fp;
  int ok = 0;

  if (Buffer == NULL || BufferSize <= 0 || idx < 0)
    return 0;
  if (ini_openread(Filename, &fp)) {
    ok = getkeystring(&fp, NULL, NULL, idx, -1, Buffer, BufferSize);
    (void)ini_close(&fp);
  } /* if */
  if (!ok)
    *Buffer = '\0';
  return _tcslen(Buffer);
}

/** ini_getkey()
 * \param Section     the name of the section to browse through, or NULL to
 *                    browse through the keys outside any section
 * \param idx         the zero-based sequence number of the key to return
 * \param Buffer      a pointer to the buffer to copy into
 * \param BufferSize  the maximum number of characters to copy
 * \param Filename    the name and full path of the .ini file to read from
 *
 * \return            the number of characters copied into the supplied buffer
 */
int  ini_getkey(const TCHAR *Section, int idx, TCHAR *Buffer, int BufferSize, const TCHAR *Filename)
{
  INI_FILETYPE fp;
  int ok = 0;

  if (Buffer == NULL || BufferSize <= 0 || idx < 0)
    return 0;
  if (ini_openread(Filename, &fp)) {
    ok = getkeystring(&fp, Section, NULL, -1, idx, Buffer, BufferSize);
    (void)ini_close(&fp);
  } /* if */
  if (!ok)
    *Buffer = '\0';
  return _tcslen(Buffer);
}


#if ! defined INI_READONLY
static void ini_tempname(TCHAR *dest, const TCHAR *source, int maxlength)
{
  TCHAR *p;

  save_strncpy(dest, source, maxlength, QUOTE_NONE);
  p = _tcsrchr(dest, '\0');
  assert(p != NULL);
  *(p - 1) = '~';
}

static enum quote_option check_enquote(const TCHAR *Value)
{
  const TCHAR *p;

  /* run through the value, if it has trailing spaces, or '"', ';' or '#'
   * characters, enquote it
   */
  assert(Value != NULL);
  for (p = Value; *p != '\0' && *p != '"' && *p != ';' && *p != '#'; p++)
    /* nothing */;
  return (*p != '\0' || (p > Value && *(p - 1) == ' ')) ? QUOTE_ENQUOTE : QUOTE_NONE;
}

static void writesection(TCHAR *LocalBuffer, const TCHAR *Section, INI_FILETYPE *fp)
{
  TCHAR *p;

  if (Section != NULL && _tcslen(Section) > 0) {
    LocalBuffer[0] = '[';
    save_strncpy(LocalBuffer + 1, Section, INI_BUFFERSIZE - 4, QUOTE_NONE);  /* -1 for '[', -1 for ']', -2 for '\r\n' */
    p = _tcsrchr(LocalBuffer, '\0');
    assert(p != NULL);
    *p++ = ']';
    _tcscpy_s(p,INI_BUFFERSIZE, INI_LINETERM); /* copy line terminator (typically "\n") */
    (void)ini_write(LocalBuffer, fp);
  } /* if */
}

static void writekey(TCHAR *LocalBuffer, const TCHAR *Key, const TCHAR *Value, INI_FILETYPE *fp)
{
  TCHAR *p;
  enum quote_option option = check_enquote(Value);
  save_strncpy(LocalBuffer, Key, INI_BUFFERSIZE - 3, QUOTE_NONE);  /* -1 for '=', -2 for '\r\n' */
  p = _tcsrchr(LocalBuffer, '\0');
  assert(p != NULL);
  *p++ = '=';
  save_strncpy(p, Value, INI_BUFFERSIZE - (p - LocalBuffer) - 2, option); /* -2 for '\r\n' */
  p = _tcsrchr(LocalBuffer, '\0');
  assert(p != NULL);
  _tcscpy_s(p,INI_BUFFERSIZE, INI_LINETERM); /* copy line terminator (typically "\n") */
  (void)ini_write(LocalBuffer, fp);
}

static int cache_accum(const TCHAR *string, int *size, int max)
{
  int len = _tcslen(string);
  if (*size + len >= max)
    return 0;
  *size += len;
  return 1;
}

static int cache_flush(TCHAR *buffer, int *size,
                      INI_FILETYPE *rfp, INI_FILETYPE *wfp, INI_FILEPOS *mark)
{
  int pos = 0;

  (void)ini_seek(rfp, mark);
  assert(buffer != NULL);
  buffer[0] = '\0';
  assert(size != NULL);
  while (pos < *size) {
    (void)ini_read(buffer + pos, INI_BUFFERSIZE - pos, rfp);
    pos += _tcslen(buffer + pos);
    assert(pos <= *size);
  } /* while */
  if (buffer[0] != '\0')
    (void)ini_write(buffer, wfp);
  (void)ini_tell(rfp, mark);  /* update mark */
  *size = 0;
  /* return whether the buffer ended with a line termination */
  return (_tcscmp(buffer + pos - _tcslen(INI_LINETERM), INI_LINETERM) == 0);
}

static int close_rename(INI_FILETYPE *rfp, INI_FILETYPE *wfp, const TCHAR *filename, TCHAR *buffer)
{
  (void)ini_close(rfp);
  (void)ini_close(wfp);
  (void)ini_remove(filename);
  (void)ini_tempname(buffer, filename, INI_BUFFERSIZE);
  (void)ini_rename(buffer, filename);
  return 1;
}

/** ini_puts()
 * \param Section     the name of the section to write the string in
 * \param Key         the name of the entry to write, or NULL to erase all keys in the section
 * \param Value       a pointer to the buffer the string, or NULL to erase the key
 * \param Filename    the name and full path of the .ini file to write to
 *
 * \return            1 if successful, otherwise 0
 */
int ini_puts(const TCHAR *Section, const TCHAR *Key, const TCHAR *Value, const TCHAR *Filename)
{
  INI_FILETYPE rfp;
  INI_FILETYPE wfp;
  INI_FILEPOS mark;
  TCHAR *sp, *ep;
  TCHAR LocalBuffer[INI_BUFFERSIZE];
  int len, match, flag, cachelen;

  assert(Filename != NULL);
  if (!ini_openread(Filename, &rfp)) {
    /* If the .ini file doesn't exist, make a new file */
    if (Key != NULL && Value != NULL) {
      if (!ini_openwrite(Filename, &wfp))
        return 0;
      writesection(LocalBuffer, Section, &wfp);
      writekey(LocalBuffer, Key, Value, &wfp);
      (void)ini_close(&wfp);
    } /* if */
    return 1;
  } /* if */

  /* If parameters Key and Value are valid (so this is not an "erase" request)
   * and the setting already exists and it already has the correct value, do
   * nothing. This early bail-out avoids rewriting the INI file for no reason.
   */
  if (Key != NULL && Value != NULL) {
    (void)ini_tell(&rfp, &mark);
    match = getkeystring(&rfp, Section, Key, -1, -1, LocalBuffer, sizearray(LocalBuffer));
    if (match && _tcscmp(LocalBuffer,Value) == 0) {
      (void)ini_close(&rfp);
      return 1;
    } /* if */
    /* key not found, or different value -> proceed (but rewind the input file first) */
    (void)ini_seek(&rfp, &mark);
  } /* if */

  /* Get a temporary file name to copy to. Use the existing name, but with
   * the last character set to a '~'.
   */
  ini_tempname(LocalBuffer, Filename, INI_BUFFERSIZE);
  if (!ini_openwrite(LocalBuffer, &wfp)) {
    (void)ini_close(&rfp);
    return 0;
  } /* if */
  (void)ini_tell(&rfp, &mark);
  cachelen = 0;

  /* Move through the file one line at a time until a section is
   * matched or until EOF. Copy to temp file as it is read.
   */
  len = (Section != NULL) ? _tcslen(Section) : 0;
  if (len > 0) {
    do {
      if (!ini_read(LocalBuffer, INI_BUFFERSIZE, &rfp)) {
        /* Failed to find section, so add one to the end */
        flag = cache_flush(LocalBuffer, &cachelen, &rfp, &wfp, &mark);
        if (Key!=NULL && Value!=NULL) {
          if (!flag)
            (void)ini_write(INI_LINETERM, &wfp);  /* force a new line behind the last line of the INI file */
          writesection(LocalBuffer, Section, &wfp);
          writekey(LocalBuffer, Key, Value, &wfp);
        } /* if */
        return close_rename(&rfp, &wfp, Filename, LocalBuffer);  /* clean up and rename */
      } /* if */
      /* Copy the line from source to dest, but not if this is the section that
       * we are looking for and this section must be removed
       */
      sp = skipleading(LocalBuffer);
      ep = _tcschr(sp, ']');
      match = (*sp == '[' && ep != NULL && (int)(ep-sp-1) == len && _tcsnicmp(sp + 1,Section,len) == 0);
      if (!match || Key != NULL) {
        if (!cache_accum(LocalBuffer, &cachelen, INI_BUFFERSIZE)) {
          cache_flush(LocalBuffer, &cachelen, &rfp, &wfp, &mark);
          (void)ini_read(LocalBuffer, INI_BUFFERSIZE, &rfp);
          cache_accum(LocalBuffer, &cachelen, INI_BUFFERSIZE);
        } /* if */
      } /* if */
    } while (!match);
  } /* if */
  cache_flush(LocalBuffer, &cachelen, &rfp, &wfp, &mark);
  /* when deleting a section, the section head that was just found has not been
   * copied to the output file, but because this line was not "accumulated" in
   * the cache, the position in the input file was reset to the point just
   * before the section; this must now be skipped (again)
   */
  if (Key == NULL) {
    (void)ini_read(LocalBuffer, INI_BUFFERSIZE, &rfp);
    (void)ini_tell(&rfp, &mark);
  } /* if */

  /* Now that the section has been found, find the entry. Stop searching
   * upon leaving the section's area. Copy the file as it is read
   * and create an entry if one is not found.
   */
  len = (Key!=NULL) ? _tcslen(Key) : 0;
  for( ;; ) {
    if (!ini_read(LocalBuffer, INI_BUFFERSIZE, &rfp)) {
      /* EOF without an entry so make one */
      flag = cache_flush(LocalBuffer, &cachelen, &rfp, &wfp, &mark);
      if (Key!=NULL && Value!=NULL) {
        if (!flag)
          (void)ini_write(INI_LINETERM, &wfp);  /* force a new line behind the last line of the INI file */
        writekey(LocalBuffer, Key, Value, &wfp);
      } /* if */
      return close_rename(&rfp, &wfp, Filename, LocalBuffer);  /* clean up and rename */
    } /* if */
    sp = skipleading(LocalBuffer);
    ep = _tcschr(sp, '='); /* Parse out the equal sign */
    if (ep == NULL)
      ep = _tcschr(sp, ':');
    match = (ep != NULL && (int)(skiptrailing(ep,sp)-sp) == len && _tcsnicmp(sp,Key,len) == 0);
    if ((Key != NULL && match) || *sp == '[')
      break;  /* found the key, or found a new section */
    /* copy other keys in the section */
    if (Key == NULL) {
      (void)ini_tell(&rfp, &mark);  /* we are deleting the entire section, so update the read position */
    } else {
      if (!cache_accum(LocalBuffer, &cachelen, INI_BUFFERSIZE)) {
        cache_flush(LocalBuffer, &cachelen, &rfp, &wfp, &mark);
        (void)ini_read(LocalBuffer, INI_BUFFERSIZE, &rfp);
        cache_accum(LocalBuffer, &cachelen, INI_BUFFERSIZE);
      } /* if */
    } /* if */
  } /* for */
  /* the key was found, or we just dropped on the next section (meaning that it
   * wasn't found); in both cases we need to write the key, but in the latter
   * case, we also need to write the line starting the new section after writing
   * the key
   */
  flag = (*sp == '[');
  cache_flush(LocalBuffer, &cachelen, &rfp, &wfp, &mark);
  if (Key != NULL && Value != NULL)
    writekey(LocalBuffer, Key, Value, &wfp);
  /* cache_flush() reset the "read pointer" to the start of the line with the
   * previous key or the new section; read it again (because writekey() destroyed
   * the buffer)
   */
  (void)ini_read(LocalBuffer, INI_BUFFERSIZE, &rfp);
  if (flag) {
    /* the new section heading needs to be copied to the output file */
    cache_accum(LocalBuffer, &cachelen, INI_BUFFERSIZE);
  } else {
    /* forget the old key line */
    (void)ini_tell(&rfp, &mark);
  } /* if */
  /* Copy the rest of the INI file */
  while (ini_read(LocalBuffer, INI_BUFFERSIZE, &rfp)) {
    if (!cache_accum(LocalBuffer, &cachelen, INI_BUFFERSIZE)) {
      cache_flush(LocalBuffer, &cachelen, &rfp, &wfp, &mark);
      (void)ini_read(LocalBuffer, INI_BUFFERSIZE, &rfp);
      cache_accum(LocalBuffer, &cachelen, INI_BUFFERSIZE);
    } /* if */
  } /* while */
  cache_flush(LocalBuffer, &cachelen, &rfp, &wfp, &mark);
  return close_rename(&rfp, &wfp, Filename, LocalBuffer);  /* clean up and rename */
}

/* Ansi C "itoa" based on Kernighan & Ritchie's "Ansi C" book. */
#define ABS(v)  ((v) < 0 ? -(v) : (v))

static void strreverse(TCHAR *str)
{
  TCHAR t;
  int i, j;

  for (i = 0, j = _tcslen(str) - 1; i < j; i++, j--) {
    t = str[i];
    str[i] = str[j];
    str[j] = t;
  } /* for */
}

static void long2str(long value, TCHAR *str)
{
  int i = 0;
  long sign = value;
  int n;

  /* generate digits in reverse order */
  do {
    n = (int)(value % 10);              /* get next lowest digit */
    str[i++] = (TCHAR)(ABS(n) + '0');   /* handle case of negative digit */
  } while (value /= 10);                /* delete the lowest digit */
  if (sign < 0)
    str[i++] = '-';
  str[i] = '\0';

  strreverse(str);
}

/** ini_putl()
 * \param Section     the name of the section to write the value in
 * \param Key         the name of the entry to write
 * \param Value       the value to write
 * \param Filename    the name and full path of the .ini file to write to
 *
 * \return            1 if successful, otherwise 0
 */
int ini_putl(const TCHAR *Section, const TCHAR *Key, long Value, const TCHAR *Filename)
{
  TCHAR LocalBuffer[32];
  long2str(Value, LocalBuffer);
  return ini_puts(Section, Key, LocalBuffer, Filename);
}

#if defined INI_REAL
/** ini_putf()
 * \param Section     the name of the section to write the value in
 * \param Key         the name of the entry to write
 * \param Value       the value to write
 * \param Filename    the name and full path of the .ini file to write to
 *
 * \return            1 if successful, otherwise 0
 */
int ini_putf(const TCHAR *Section, const TCHAR *Key, INI_REAL Value, const TCHAR *Filename)
{
  TCHAR LocalBuffer[64];
  ini_ftoa(LocalBuffer, 64,Value);
  return ini_puts(Section, Key, LocalBuffer, Filename);
}
#endif /* INI_REAL */
#endif /* !INI_READONLY */


#if defined PORTABLE_STRNICMP
int strnicmp(const TCHAR *s1, const TCHAR *s2, size_t n)
{
  register unsigned TCHAR c1, c2;

  while (n-- != 0 && (*s1 || *s2)) {
    c1 = *(const unsigned TCHAR *)s1++;
    if ('a' <= c1 && c1 <= 'z')
      c1 += ('A' - 'a');
    c2 = *(const unsigned TCHAR *)s2++;
    if ('a' <= c2 && c2 <= 'z')
      c2 += ('A' - 'a');
    if (c1 != c2)
      return c1 - c2;
  } /* while */
  return 0;
}
#endif /* PORTABLE_STRNICMP */
