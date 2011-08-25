/* Simple test program
 *
 * wcl386 -wx -d2 -q test.c minini.c
 */
#include <assert.h>
#include <stdio.h>
#include <string.h>
#include "minIni.h"

#define sizearray(a)  (sizeof(a) / sizeof((a)[0]))

const char inifile[] = "test.ini";
const char inifile2[] = "testplain.ini";

int main(void)
{
  char str[100];
  long n;
  int s, k;
  char section[50];

  /* string reading */
  n = ini_gets("first", "string", "dummy", str, sizearray(str), inifile);
  assert(n==4 && strcmp(str,"noot")==0);
  n = ini_gets("second", "string", "dummy", str, sizearray(str), inifile);
  assert(n==4 && strcmp(str,"mies")==0);
  n = ini_gets("first", "undefined", "dummy", str, sizearray(str), inifile);
  assert(n==5 && strcmp(str,"dummy")==0);
  /* ----- */
  n = ini_gets("", "string", "dummy", str, sizearray(str), inifile2);
  assert(n==4 && strcmp(str,"noot")==0);
  n = ini_gets(NULL, "string", "dummy", str, sizearray(str), inifile2);
  assert(n==4 && strcmp(str,"noot")==0);
  /* ----- */
  printf("1. String reading tests passed\n");

  /* value reading */
  n = ini_getl("first", "val", -1, inifile);
  assert(n==1);
  n = ini_getl("second", "val", -1, inifile);
  assert(n==2);
  n = ini_getl("first", "undefined", -1, inifile);
  assert(n==-1);
  /* ----- */
  n = ini_getl(NULL, "val", -1, inifile2);
  assert(n==1);
  /* ----- */
  printf("2. Value reading tests passed\n");

  /* string writing */
  n = ini_puts("first", "alt", "flagged as \"correct\"", inifile);
  assert(n==1);
  n = ini_gets("first", "alt", "dummy", str, sizearray(str), inifile);
  assert(n==20 && strcmp(str,"flagged as \"correct\"")==0);
  /* ----- */
  n = ini_puts("second", "alt", "correct", inifile);
  assert(n==1);
  n = ini_gets("second", "alt", "dummy", str, sizearray(str), inifile);
  assert(n==7 && strcmp(str,"correct")==0);
  /* ----- */
  n = ini_puts("third", "test", "correct", inifile);
  assert(n==1);
  n = ini_gets("third", "test", "dummy", str, sizearray(str), inifile);
  assert(n==7 && strcmp(str,"correct")==0);
  /* ----- */
  n = ini_puts("second", "alt", "overwrite", inifile);
  assert(n==1);
  n = ini_gets("second", "alt", "dummy", str, sizearray(str), inifile);
  assert(n==9 && strcmp(str,"overwrite")==0);
  /* ----- */
  n = ini_puts(NULL, "alt", "correct", inifile2);
  assert(n==1);
  n = ini_gets(NULL, "alt", "dummy", str, sizearray(str), inifile2);
  assert(n==7 && strcmp(str,"correct")==0);
  /* ----- */
  printf("3. String writing tests passed\n");

  /* section/key enumeration */
  for (s = 0; ini_getsection(s, section, sizearray(section), inifile) > 0; s++) {
    printf("[%s]\n", section);
    for (k = 0; ini_getkey(section, k, str, sizearray(str), inifile) > 0; k++) {
      printf("\t%s\n", str);
    } /* for */
  } /* for */

  /* string deletion */
  n = ini_puts("first", "alt", NULL, inifile);
  assert(n==1);
  n = ini_puts("second", "alt", NULL, inifile);
  assert(n==1);
  n = ini_puts("third", NULL, NULL, inifile);
  assert(n==1);
  /* ----- */
  n = ini_puts(NULL, "alt", NULL, inifile2);
  assert(n==1);

  return 0;
}

