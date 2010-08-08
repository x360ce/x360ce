/*  x360ce - XBOX360 Controler Emulator
 *  Copyright (C) 2002-2010 ToCA Edit
 *
 *  x360ce is free software: you can redistribute it and/or modify it under the terms
 *  of the GNU Lesser General Public License as published by the Free Software Found-
 *  ation, either version 3 of the License, or (at your option) any later version.
 *
 *  x360ce is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
 *  without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR
 *  PURPOSE.  See the GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License along with x360ce.
 *  If not, see <http://www.gnu.org/licenses/>.
 */

extern WORD wFakeAPI;
extern WORD wFakeWMI;
extern WORD wFakeWMI_NOPIDVID;
extern WORD wFakeDI;
extern WORD wFakeWinTrust;

extern WORD wFakeVID;
extern WORD wFakePID;

void FakeWMI(bool state);
void FakeDI(bool state);
void FakeWinTrust(bool state);
