#include "stdafx.h"
#include "Common.h"

#include <set>

#include "Config.h"
#include "Sdl.h"
#include "resource.h"

namespace Sdl
{
	namespace
	{
		#pragma region Rows

		/// <summary>One row of a database: its text, and its vendor and product as vendor &lt;&lt; 16 | product.</summary>
		struct Row
		{
			const char* text;
			size_t length;
			u32 key;
		};

		bool KeyLess(const Row& a, const Row& b)
		{
			return a.key < b.key;
		}

		int Hex(char c)
		{
			if (c >= '0' && c <= '9') return c - '0';
			if (c >= 'a' && c <= 'f') return c - 'a' + 10;
			if (c >= 'A' && c <= 'F') return c - 'A' + 10;
			return -1;
		}

		/// <summary>A two-byte number written little-endian as four hex digits.</summary>
		u32 Le16(const char* hex)
		{
			return (u32)(Hex(hex[0]) << 4 | Hex(hex[1])) | (u32)(Hex(hex[2]) << 4 | Hex(hex[3])) << 8;
		}

		/// <summary>The vendor and product an SDL GUID carries, in the standard form or as DirectInput's product GUID.</summary>
		/// <remarks>
		/// Standard: <c>{bus}{crc}{vendor}0000{product}0000{version}{driver}{data}</c>. Before SDL 2.0.5:
		/// <c>{vendor}{product}000000000000504944564944</c>, the last six bytes "PIDVID". As SdlGuid in x360ce 4.
		/// </remarks>
		bool GuidKey(const char* guid, size_t length, u32* key)
		{
			if (length != 32)
				return false;
			for (size_t i = 0; i < length; ++i)
				if (Hex(guid[i]) < 0)
					return false;
			u32 vendor, product;
			if (strncmp(guid + 8, "000000000000", 12) == 0 && strncmp(guid + 20, "504944564944", 12) == 0)
			{
				vendor = Le16(guid);
				product = Le16(guid + 4);
			}
			else if (strncmp(guid + 12, "0000", 4) == 0 && strncmp(guid + 20, "0000", 4) == 0)
			{
				vendor = Le16(guid + 8);
				product = Le16(guid + 16);
			}
			else
				return false;
			*key = vendor << 16 | product;
			return *key != 0;
		}

		bool EqualsIgnoreCase(const char* a, size_t length, const char* b)
		{
			return strlen(b) == length && _strnicmp(a, b, length) == 0;
		}

		/// <summary>Adds the rows of a text that x360ce reads: for Windows or for no platform, with a vendor and product.</summary>
		void ReadRows(const char* text, size_t size, std::vector<Row>* rows)
		{
			size_t at = 0;
			// A UTF-8 byte order mark.
			if (size >= 3 && (u8)text[0] == 0xEF && (u8)text[1] == 0xBB && (u8)text[2] == 0xBF)
				at = 3;
			while (at < size)
			{
				size_t end = at;
				while (end < size && text[end] != '\n')
					++end;
				size_t first = at, last = end;
				at = end + 1;
				while (first < last && (text[first] == ' ' || text[first] == '\t'))
					++first;
				while (last > first && (text[last - 1] == '\r' || text[last - 1] == ' ' || text[last - 1] == '\t'))
					--last;
				if (first == last || text[first] == '#')
					continue;
				const char* line = text + first;
				size_t length = last - first;
				const char* comma = (const char*)memchr(line, ',', length);
				Row row = { line, length, 0 };
				if (!comma || !GuidKey(line, comma - line, &row.key))
					continue;
				// A row for another platform; a row naming none is SDL's on every platform.
				std::string body(line, length);
				size_t platform = body.find(",platform:");
				if (platform != std::string::npos)
				{
					size_t from = platform + 10;
					size_t to = body.find(',', from);
					if (to == std::string::npos)
						to = body.size();
					if (!EqualsIgnoreCase(body.c_str() + from, to - from, "Windows"))
						continue;
				}
				rows->push_back(row);
			}
		}

		/// <summary>The rows carried in the library, and those of a file of the same name found where x360ce.ini is looked for.</summary>
		class Database
		{
		public:
			std::vector<Row> builtIn;
			std::string fileText;
			std::vector<Row> file;

			/// <param name="resourceId">The rows the library carries.</param>
			/// <param name="fileName">The file a person's own rows are in: gamecontrollerdb.txt or gamecontrollerdb_x360ce.windows.txt.</param>
			Database(WORD resourceId, const char* fileName)
			{
				// The library's own rows, sorted by vendor and product when they were written.
				HMODULE module = CurrentModule();
				HRSRC resource = FindResource(module, MAKEINTRESOURCE(resourceId), RT_RCDATA);
				HGLOBAL loaded = resource ? LoadResource(module, resource) : NULL;
				const char* text = loaded ? (const char*)LockResource(loaded) : nullptr;
				if (text)
					ReadRows(text, SizeofResource(module, resource), &builtIn);
				// A person's own rows win over the library's for a device they have a row for.
				std::string path;
				if (!FullPathFromPath(&path, fileName))
				{
					path = fileName;
					if (!CheckCommonDirectory(&path, "x360ce"))
						path.clear();
				}
				if (!path.empty() && ReadFileText(path, &fileText))
				{
					ReadRows(fileText.c_str(), fileText.size(), &file);
					std::stable_sort(file.begin(), file.end(), KeyLess);
					PrintLog("Using database file \"%s\": %u rows", path.c_str(), (u32)file.size());
				}
			}

		private:
			static bool ReadFileText(const std::string& path, std::string* text)
			{
				HANDLE hFile = CreateFileA(path.c_str(), GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);
				if (hFile == INVALID_HANDLE_VALUE)
					return false;
				LARGE_INTEGER size;
				bool ok = GetFileSizeEx(hFile, &size) && size.QuadPart < 64 * 1024 * 1024;
				if (ok)
				{
					text->resize((size_t)size.QuadPart);
					DWORD read = 0;
					ok = size.QuadPart == 0 || (ReadFile(hFile, &(*text)[0], (DWORD)size.QuadPart, &read, NULL) && read == size.QuadPart);
				}
				CloseHandle(hFile);
				return ok;
			}
		};

		Database& GetDatabase()
		{
			static Database database(IDR_SDLDB, "gamecontrollerdb.txt");
			return database;
		}

		Database& GetX360ceDatabase()
		{
			static Database database(IDR_X360CEDB, "gamecontrollerdb_x360ce.windows.txt");
			return database;
		}

		/// <summary>Splits text at a character.</summary>
		std::vector<std::string> Split(const std::string& text, char separator)
		{
			std::vector<std::string> parts;
			size_t from = 0;
			for (;;)
			{
				size_t to = text.find(separator, from);
				parts.push_back(text.substr(from, to == std::string::npos ? std::string::npos : to - from));
				if (to == std::string::npos)
					return parts;
				from = to + 1;
			}
		}

		std::string Trim(const std::string& text)
		{
			size_t first = text.find_first_not_of(" \t\r\n");
			if (first == std::string::npos)
				return std::string();
			size_t last = text.find_last_not_of(" \t\r\n");
			return text.substr(first, last - first + 1);
		}

		std::string Lower(std::string text)
		{
			for (auto& c : text)
				c = (char)tolower((unsigned char)c);
			return text;
		}

		/// <summary>How alike two names are: equal, one holding the other, then the words they share. As x360ce 4 chooses among rows.</summary>
		int NameScore(const std::string& rowName, const std::string& deviceName)
		{
			std::string a = Lower(Trim(rowName));
			std::string b = Lower(Trim(deviceName));
			if (a.empty() || b.empty())
				return 0;
			if (a == b)
				return 1000;
			if (a.find(b) != std::string::npos || b.find(a) != std::string::npos)
				return 500;
			auto words = [](const std::string& text)
			{
				std::set<std::string> list;
				std::string word;
				for (char c : text + " ")
				{
					if (strchr(" -_()/.", c))
					{
						if (!word.empty())
							list.insert(word);
						word.clear();
					}
					else
						word.push_back(c);
				}
				return list;
			};
			std::set<std::string> wordsA = words(a), wordsB = words(b);
			int shared = 0;
			for (const auto& w : wordsA)
				if (wordsB.count(w))
					++shared;
			return shared;
		}

		/// <summary>The row's name: the text between its first and second comma.</summary>
		std::string RowName(const Row& row)
		{
			std::string text(row.text, row.length);
			size_t first = text.find(',');
			size_t second = first == std::string::npos ? std::string::npos : text.find(',', first + 1);
			return second == std::string::npos ? std::string() : text.substr(first + 1, second - first - 1);
		}

		/// <summary>The row of a vendor and product whose name is nearest the device's, the first written when two are as near; null for none.</summary>
		const Row* Best(const std::vector<Row>& rows, u32 key, const std::string& name)
		{
			Row probe = { nullptr, 0, key };
			auto range = std::equal_range(rows.begin(), rows.end(), probe, KeyLess);
			const Row* best = nullptr;
			int bestScore = -1;
			for (auto it = range.first; it != range.second; ++it)
			{
				int score = NameScore(RowName(*it), name);
				if (score > bestScore)
				{
					best = &*it;
					bestScore = score;
				}
			}
			return best;
		}

		/// <summary>The row for a device in a database: a person's own first, then the library's.</summary>
		std::string FindIn(Database& db, const Device& device)
		{
			u32 key = (u32)device.vendorId << 16 | device.productId;
			if (key == 0)
				return std::string();
			const Row* row = Best(db.file, key, device.name);
			if (!row)
				row = Best(db.builtIn, key, device.name);
			return row ? std::string(row->text, row->length) : std::string();
		}

		#pragma endregion

		#pragma region Bindings

		enum Kind { KIND_BUTTON, KIND_AXIS, KIND_HAT };

		/// <summary>One binding: the control of the device a field reads.</summary>
		struct Binding
		{
			std::string field;
			Kind kind = KIND_BUTTON;
			/// <summary>Button or axis number from 0, or the hat's.</summary>
			int index = 0;
			/// <summary>The hat's direction bit: 1 up, 2 right, 4 down, 8 left.</summary>
			int hatMask = 0;
			/// <summary>+1 the upper half of the axis, -1 the lower half, 0 all of it.</summary>
			int half = 0;
			bool inverted = false;
		};

		/// <summary>A control the library maps, as a mapping slot holds it.</summary>
		struct Value
		{
			Config::MappingType type = Config::NONE;
			s8 id = 0;
		};

		bool Digits(const std::string& text, size_t from, size_t to, int* number)
		{
			if (from >= to || to - from > 9)
				return false;
			int n = 0;
			for (size_t i = from; i < to; ++i)
			{
				if (text[i] < '0' || text[i] > '9')
					return false;
				n = n * 10 + (text[i] - '0');
			}
			*number = n;
			return true;
		}

		/// <summary>Reads a binding: <c>bK</c>, <c>hH.V</c>, <c>aK</c>, <c>+aK</c>, <c>-aK</c> or <c>aK~</c>.</summary>
		bool ParseBinding(const std::string& field, const std::string& text, Binding* b)
		{
			std::string t = Trim(text);
			b->field = field;
			if (!t.empty() && (t[0] == '+' || t[0] == '-'))
			{
				b->half = t[0] == '+' ? 1 : -1;
				t = t.substr(1);
			}
			if (!t.empty() && t.back() == '~')
			{
				b->inverted = true;
				t.pop_back();
			}
			if (t.size() < 2)
				return false;
			int n, mask;
			if (t[0] == 'a' && Digits(t, 1, t.size(), &n))
			{
				b->kind = KIND_AXIS;
				b->index = n;
				return true;
			}
			if (b->half != 0 || b->inverted)
				return false;
			if (t[0] == 'b' && Digits(t, 1, t.size(), &n))
			{
				b->kind = KIND_BUTTON;
				b->index = n;
				return true;
			}
			size_t dot = t.find('.');
			if (t[0] == 'h' && dot != std::string::npos && dot > 1 && Digits(t, 1, dot, &n) && Digits(t, dot + 1, t.size(), &mask))
			{
				b->kind = KIND_HAT;
				b->index = n;
				b->hatMask = mask;
				return true;
			}
			return false;
		}

		/// <summary>The library's axis SDL axis <c>aK</c> is on the device: Axis 1 to 6 or Slider 1 to 2.</summary>
		bool TryGetAxis(const Device& device, int sdlIndex, bool* slider, int* index)
		{
			int n = 0;
			for (int i = 0; i < 8; ++i)
			{
				bool isSlider = i >= 6;
				bool present = isSlider ? (device.sliderMask & (1u << (i - 6))) != 0 : (device.axisMask & (1u << i)) != 0;
				if (!present)
					continue;
				if (n == sdlIndex)
				{
					*slider = isSlider;
					*index = isSlider ? i - 5 : i + 1;
					return true;
				}
				++n;
			}
			return false;
		}

		/// <summary>The D-Pad direction from 1 of a hat binding: POV H+1's up, right, down, left; nought for no single direction.</summary>
		int HatButton(const Binding& b)
		{
			static const int masks[] = { 1, 2, 4, 8 };
			for (int d = 0; d < 4; ++d)
				if (masks[d] == b.hatMask)
					return b.index * 4 + d + 1;
			return 0;
		}

		/// <summary>Whether the device has the control a binding reads.</summary>
		bool Has(const Device& device, const Binding& b)
		{
			if (b.kind == KIND_BUTTON)
				return device.buttonCount == 0 || (u32)b.index < device.buttonCount;
			if (b.kind == KIND_HAT)
				return HatButton(b) > 0 && (device.povCount == 0 || (u32)b.index < device.povCount);
			bool slider;
			int index;
			return TryGetAxis(device, b.index, &slider, &index);
		}

		bool Make(Config::MappingType type, int id, Value* v)
		{
			if (id == 0 || id > 127 || id < -127)
				return false;
			v->type = type;
			v->id = (s8)id;
			return true;
		}

		/// <summary>The control a button slot takes from a binding. A whole axis presses a button over its upper half, as SDL reads it; inverted, over its lower half.</summary>
		bool ButtonValue(const Device& device, const Binding& b, Value* v)
		{
			if (b.kind == KIND_BUTTON)
				return Make(Config::DIGITAL, b.index + 1, v);
			if (b.kind == KIND_HAT)
				return Make(Config::DPADBUTTON, HatButton(b), v);
			bool slider;
			int index;
			if (!TryGetAxis(device, b.index, &slider, &index) || (b.half != 0 && b.inverted))
				return false;
			bool upper = b.half > 0 || (b.half == 0 && !b.inverted);
			return Make(slider ? Config::HSLIDER : Config::HAXIS, upper ? index : -index, v);
		}

		/// <summary>The control a trigger slot takes from a binding.</summary>
		bool TriggerValue(const Device& device, const Binding& b, Value* v)
		{
			if (b.kind != KIND_AXIS)
				return ButtonValue(device, b, v);
			bool slider;
			int index;
			if (!TryGetAxis(device, b.index, &slider, &index) || (b.half != 0 && b.inverted))
				return false;
			if (b.half != 0)
				return Make(slider ? Config::HSLIDER : Config::HAXIS, b.half > 0 ? index : -index, v);
			return Make(slider ? Config::SLIDER : Config::AXIS, b.inverted ? -index : index, v);
		}

		/// <summary>The control a stick axis takes from a whole axis; SDL's Y grows downward, the Xbox controller's upward.</summary>
		bool AxisValue(const Device& device, const Binding& b, bool flip, Value* v)
		{
			bool slider;
			int index;
			if (!TryGetAxis(device, b.index, &slider, &index))
				return false;
			bool inverted = b.inverted != flip;
			return Make(slider ? Config::SLIDER : Config::AXIS, inverted ? -index : index, v);
		}

		/// <summary>A stick half from a binding, read 0 at rest to all the way: a button, a hat direction or half an axis.</summary>
		bool HalfValue(const Device& device, const Binding& b, Value* v)
		{
			if (b.kind != KIND_AXIS)
				return ButtonValue(device, b, v);
			bool slider;
			int index;
			if (b.half == 0 || b.inverted || !TryGetAxis(device, b.index, &slider, &index))
				return false;
			return Make(slider ? Config::HSLIDER : Config::HAXIS, b.half > 0 ? index : -index, v);
		}

		#pragma endregion

		#pragma region Slots

		/// <summary>SDL's gamepad buttons, with the library's button slot (Config::buttonNames order) or KEY_GUIDE for the guide.</summary>
		struct Field
		{
			const char* name;
			u32 slot;
		};

		const Field ButtonFields[] =
		{
			{ "a", 0 }, { "b", 1 }, { "x", 2 }, { "y", 3 },
			{ "leftshoulder", 4 }, { "rightshoulder", 5 }, { "back", 6 }, { "start", 7 },
			{ "leftstick", 8 }, { "rightstick", 9 },
			{ "dpup", 10 }, { "dpdown", 11 }, { "dpleft", 12 }, { "dpright", 13 },
			{ "guide", Config::KEY_GUIDE },
		};

		const Field TriggerFields[] = { { "lefttrigger", 0 }, { "righttrigger", 1 } };

		const Field StickFields[] = { { "leftx", 0 }, { "lefty", 1 }, { "rightx", 2 }, { "righty", 3 } };

		int FindField(const Field* fields, size_t count, const std::string& name)
		{
			for (size_t i = 0; i < count; ++i)
				if (name == fields[i].name)
					return (int)fields[i].slot;
			return -1;
		}

		/// <summary>The mapping being filled, and which of its controls are filled, by their Config::KEY_ bits; the first binding of a control wins.</summary>
		struct Builder
		{
			Config::Mapping* mapping;
			u32 filled;

			bool Take(u32 bit)
			{
				if (filled & (1u << bit))
					return false;
				filled |= 1u << bit;
				return true;
			}

			void Button(u32 slot, const Value& v)
			{
				if (slot == Config::KEY_GUIDE)
				{
					// The library's guide button is a button, counted from nought; no Windows row puts it elsewhere.
					if (v.type == Config::DIGITAL && Take(Config::KEY_GUIDE))
						mapping->guide = v.id - 1;
					return;
				}
				if (Take(Config::KEY_BUTTON + slot))
				{
					mapping->Button[slot].type = v.type;
					mapping->Button[slot].id = v.id;
				}
			}

			void Trigger(u32 slot, const Value& v)
			{
				if (Take(Config::KEY_TRIGGER + slot))
				{
					mapping->Trigger[slot].type = v.type;
					mapping->Trigger[slot].id = v.id;
				}
			}

			void Axis(u32 stick, const Value& v)
			{
				if (Take(Config::KEY_AXIS + stick))
				{
					mapping->Axis[stick].analogType = v.type;
					mapping->Axis[stick].id = v.id;
				}
			}

			void Direction(u32 stick, bool positive, const Value& v)
			{
				if (positive && Take(Config::KEY_AXIS_POSITIVE + stick))
				{
					mapping->Axis[stick].positiveType = v.type;
					mapping->Axis[stick].positiveButtonID = v.id;
				}
				if (!positive && Take(Config::KEY_AXIS_NEGATIVE + stick))
				{
					mapping->Axis[stick].negativeType = v.type;
					mapping->Axis[stick].negativeButtonID = v.id;
				}
			}
		};

		/// <summary>Four D-Pad directions on one hat, as a hat reports them, become the D-Pad.</summary>
		void JoinDPad(Builder* builder, const std::vector<std::pair<u32, Binding>>& buttons)
		{
			// SDL's hat bit order: 1 up, 2 right, 4 down, 8 left; and the library's D-Pad button slots.
			static const char* const fields[] = { "dpup", "dpright", "dpdown", "dpleft" };
			static const u32 slots[] = { 10, 13, 11, 12 };
			const Binding* hats[4] = {};
			for (int d = 0; d < 4; ++d)
			{
				for (const auto& b : buttons)
				{
					if (b.second.field == fields[d])
					{
						hats[d] = &b.second;
						break;
					}
				}
				if (!hats[d] || hats[d]->kind != KIND_HAT || hats[d]->index != hats[0]->index || hats[d]->hatMask != 1 << d)
					return;
			}
			for (int d = 0; d < 4; ++d)
			{
				builder->mapping->Button[slots[d]].type = Config::NONE;
				builder->mapping->Button[slots[d]].id = 0;
				builder->filled &= ~(1u << (Config::KEY_BUTTON + slots[d]));
			}
			if (hats[0]->index + 1 <= 127 && builder->Take(Config::KEY_DPAD))
				builder->mapping->DpadPOV = (s8)(hats[0]->index + 1);
		}

		/// <summary>Fills a stick's axis, or its two directions, from the whole axis and its two halves, SDL's low end first.</summary>
		/// <remarks>
		/// SDL's low end of X is left and of Y up: the stick's negative direction for X, its positive for Y. Halves on two
		/// different axes, a formula in x360ce 4, are written into the two directions as half axes.
		/// </remarks>
		void SetStick(Builder* builder, const Device& device, u32 stick, const Binding* whole, const Binding* low, const Binding* high)
		{
			bool flip = stick == 1 || stick == 3;
			bool lowPositive = flip;
			Value v;
			// Both halves from the two halves of one axis are that axis.
			if (!whole && low && high && low->kind == KIND_AXIS && high->kind == KIND_AXIS && low->index == high->index
				&& !low->inverted && !high->inverted && low->half == -high->half)
			{
				Binding axis = *low;
				axis.half = 0;
				axis.inverted = low->half > 0;
				if (AxisValue(device, axis, flip, &v))
				{
					builder->Axis(stick, v);
					return;
				}
			}
			if (whole)
			{
				if (whole->kind == KIND_AXIS && whole->half == 0 && AxisValue(device, *whole, flip, &v))
					builder->Axis(stick, v);
				// Halves beside the whole axis can only be buttons that push the stick to its end.
				if (low && low->kind != KIND_AXIS && ButtonValue(device, *low, &v))
					builder->Direction(stick, lowPositive, v);
				if (high && high->kind != KIND_AXIS && ButtonValue(device, *high, &v))
					builder->Direction(stick, !lowPositive, v);
				return;
			}
			bool axes = (low && low->kind == KIND_AXIS) || (high && high->kind == KIND_AXIS);
			if (!axes)
			{
				// Buttons and hat directions push the stick to its end.
				if (low && ButtonValue(device, *low, &v))
					builder->Direction(stick, lowPositive, v);
				if (high && ButtonValue(device, *high, &v))
					builder->Direction(stick, !lowPositive, v);
				return;
			}
			Value lowValue, highValue;
			if ((low && !HalfValue(device, *low, &lowValue)) || (high && !HalfValue(device, *high, &highValue)))
				return;
			if (low)
				builder->Direction(stick, lowPositive, lowValue);
			if (high)
				builder->Direction(stick, !lowPositive, highValue);
		}

		#pragma endregion

		#pragma region Describe

		std::string Text(Config::MappingType type, s8 id)
		{
			if (id == 0)
				return std::string();
			std::string n = std::to_string(id);
			switch (type)
			{
			case Config::DIGITAL: return n;
			case Config::AXIS: return "a" + n;
			case Config::HAXIS: return "x" + n;
			case Config::SLIDER: return "s" + n;
			case Config::HSLIDER: return "h" + n;
			case Config::DPADBUTTON: return "d" + n;
			case Config::CBUT: return "z" + n;
			default: return std::string();
			}
		}

		/// <summary>A stick half in x360ce 4's formula: 0 at rest to 1 at the end.</summary>
		std::string Term(Config::MappingType type, s8 id)
		{
			std::string n = std::to_string(std::abs(id));
			switch (type)
			{
			case Config::DIGITAL: return "b" + n;
			case Config::DPADBUTTON: return "d" + n;
			case Config::HAXIS: return id > 0 ? "x" + n : "max(-a" + n + ",0)";
			case Config::HSLIDER: return id > 0 ? "h" + n : "max(1-2*s" + n + ",0)";
			default: return std::string();
			}
		}

		bool IsHalf(Config::MappingType type)
		{
			return type == Config::HAXIS || type == Config::HSLIDER;
		}

		#pragma endregion
	}

	void ReadDevice(IDirectInputDevice8A* device, Device* out)
	{
		DIDEVICEINSTANCEA instance = {};
		instance.dwSize = sizeof(instance);
		if (SUCCEEDED(device->GetDeviceInfo(&instance)))
			out->name = instance.tszProductName;
		DIPROPDWORD vidpid = {};
		vidpid.diph.dwSize = sizeof(vidpid);
		vidpid.diph.dwHeaderSize = sizeof(DIPROPHEADER);
		vidpid.diph.dwHow = DIPH_DEVICE;
		if (SUCCEEDED(device->GetProperty(DIPROP_VIDPID, &vidpid.diph)))
		{
			out->vendorId = LOWORD(vidpid.dwData);
			out->productId = HIWORD(vidpid.dwData);
		}
		else if (memcmp(instance.guidProduct.Data4 + 2, "PIDVID", 6) == 0)
		{
			// DirectInput's product GUID of a HID device: vendor and product, then "PIDVID".
			out->vendorId = LOWORD(instance.guidProduct.Data1);
			out->productId = HIWORD(instance.guidProduct.Data1);
		}
		DIDEVCAPS caps = {};
		caps.dwSize = sizeof(caps);
		if (SUCCEEDED(device->GetCapabilities(&caps)))
		{
			out->buttonCount = caps.dwButtons;
			out->povCount = caps.dwPOVs;
			out->forceFeedback = (caps.dwFlags & DIDC_FORCEFEEDBACK) != 0;
		}
		// Which of X, Y, Z, Rx, Ry, Rz and the two sliders the device has, where the joystick data format puts them.
		static const DWORD offsets[] =
		{
			DIJOFS_X, DIJOFS_Y, DIJOFS_Z, DIJOFS_RX, DIJOFS_RY, DIJOFS_RZ, DIJOFS_SLIDER(0), DIJOFS_SLIDER(1),
		};
		for (u32 i = 0; i < _countof(offsets); ++i)
		{
			DIDEVICEOBJECTINSTANCEA o = {};
			o.dwSize = sizeof(o);
			if (FAILED(device->GetObjectInfo(&o, offsets[i], DIPH_BYOFFSET)))
				continue;
			if (i < 6)
				out->axisMask |= 1u << i;
			else
				out->sliderMask |= 1u << (i - 6);
		}
	}

	bool IsXInputDevice(IDirectInputDevice8A* device)
	{
		DIPROPGUIDANDPATH path = {};
		path.diph.dwSize = sizeof(path);
		path.diph.dwHeaderSize = sizeof(DIPROPHEADER);
		path.diph.dwHow = DIPH_DEVICE;
		if (FAILED(device->GetProperty(DIPROP_GUIDANDPATH, &path.diph)))
			return false;
		_wcsupr_s(path.wszPath);
		return wcsstr(path.wszPath, L"IG_") != nullptr;
	}

	std::string FindRow(const Device& device)
	{
		return FindIn(GetDatabase(), device);
	}

	std::string FindX360ceRow(const Device& device)
	{
		return FindIn(GetX360ceDatabase(), device);
	}

	void ReadRowKeys(const std::string& row, IniFile* ini, const std::string& section)
	{
		// Where a controller goes is the section's to say, never a database's.
		static const char* const placeKeys[] =
		{
			"platform", "users", "ProductGUID", "InstanceGUID", "AutoMap", "UserIndex", "MapToPad",
			"PassThrough", "PassThroughIndex", "ForcesPassThrough", "Combined", "CombinedIndex",
		};
		// A row that leaves force feedback off says nothing about it: its strengths and periods, often nought, were set
		// while nothing used them. Switched on for a device with motors (Controller::ApplyAutoMapping), force feedback
		// runs at the defaults. x360ce's apps do the same (X360ceDatabase.AutoSetting).
		static const char* const forceKeys[] =
		{
			"FFBType", "ForcePercent", "SwapMotor", "LeftMotorPeriod", "LeftMotorStrength", "LeftMotorDirection",
			"RightMotorPeriod", "RightMotorStrength", "RightMotorDirection",
		};
		auto listed = [](const std::string& key, const char* const* names, size_t count)
		{
			for (size_t n = 0; n < count; ++n)
				if (_stricmp(key.c_str(), names[n]) == 0)
					return true;
			return false;
		};
		std::vector<std::pair<std::string, std::string>> keys;
		bool forceOn = false;
		std::vector<std::string> parts = Split(Trim(row), ',');
		for (size_t i = 2; i < parts.size(); ++i)
		{
			std::string part = Trim(parts[i]);
			size_t colon = part.find(':');
			if (colon == std::string::npos || colon == 0 || colon + 1 == part.size())
				continue;
			std::string key = part.substr(0, colon);
			if (listed(key, placeKeys, _countof(placeKeys)))
				continue;
			std::string value = part.substr(colon + 1);
			if (_stricmp(key.c_str(), "UseForceFeedback") == 0)
				forceOn = value == "1";
			keys.emplace_back(key, value);
		}
		for (const auto& key : keys)
			if (forceOn || !listed(key.first, forceKeys, _countof(forceKeys)))
				ini->Set(section, key.first, key.second);
	}

	int Convert(const std::string& row, const Device& device, Config::Mapping* mapping)
	{
		*mapping = Config::Mapping();
		std::vector<std::string> parts = Split(Trim(row), ',');
		if (parts.size() < 3)
			return 0;
		std::vector<std::pair<std::string, std::string>> fields;
		for (size_t i = 2; i < parts.size(); ++i)
		{
			std::string part = Trim(parts[i]);
			if (part.empty())
				continue;
			size_t colon = part.find(':');
			if (colon == std::string::npos || colon == 0)
				return 0;
			std::string key = part.substr(0, colon);
			if (key != "platform")
				fields.push_back(std::make_pair(key, part.substr(colon + 1)));
		}
		std::vector<std::pair<u32, Binding>> buttons, triggers;
		Binding sticks[4][3];
		bool stickHas[4][3] = {};
		for (const auto& field : fields)
		{
			std::string name = field.first;
			int sign = 0;
			if (name.size() > 1 && (name[0] == '+' || name[0] == '-'))
			{
				sign = name[0] == '+' ? 1 : -1;
				name = name.substr(1);
			}
			int button = FindField(ButtonFields, _countof(ButtonFields), name);
			int trigger = FindField(TriggerFields, _countof(TriggerFields), name);
			int stick = FindField(StickFields, _countof(StickFields), name);
			// Fields that are not parts of the pad, such as the row's face labels, and parts with no place in the library.
			if (button < 0 && trigger < 0 && stick < 0)
				continue;
			Binding b;
			if (!ParseBinding(field.first, field.second, &b) || !Has(device, b) || (sign != 0 && stick < 0 && !(sign > 0 && trigger >= 0)))
				continue;
			if (button >= 0)
				buttons.push_back(std::make_pair((u32)button, b));
			else if (trigger >= 0)
				triggers.push_back(std::make_pair((u32)trigger, b));
			else
			{
				int slot = sign == 0 ? 0 : sign < 0 ? 1 : 2;
				if (!stickHas[stick][slot])
				{
					sticks[stick][slot] = b;
					stickHas[stick][slot] = true;
				}
			}
		}
		Builder builder = { mapping, 0 };
		Value v;
		for (const auto& b : buttons)
			if (ButtonValue(device, b.second, &v))
				builder.Button(b.first, v);
		for (const auto& b : triggers)
			if (TriggerValue(device, b.second, &v))
				builder.Trigger(b.first, v);
		JoinDPad(&builder, buttons);
		for (u32 s = 0; s < 4; ++s)
			SetStick(&builder, device, s, stickHas[s][0] ? &sticks[s][0] : nullptr,
				stickHas[s][1] ? &sticks[s][1] : nullptr, stickHas[s][2] ? &sticks[s][2] : nullptr);
		int count = 0;
		for (u32 bits = builder.filled; bits; bits &= bits - 1)
			++count;
		return count;
	}

	std::string Describe(const Config::Mapping& mapping)
	{
		static const char* const buttonBoxes[] =
		{
			"ButtonA", "ButtonB", "ButtonX", "ButtonY", "LeftShoulder", "RightShoulder", "ButtonBack", "ButtonStart",
			"LeftThumbButton", "RightThumbButton", "DPadUp", "DPadDown", "DPadLeft", "DPadRight",
		};
		static const char* const axisBoxes[] = { "LeftThumbAxisX", "LeftThumbAxisY", "RightThumbAxisX", "RightThumbAxisY" };
		static const char* const positiveBoxes[] = { "LeftThumbRight", "LeftThumbUp", "RightThumbRight", "RightThumbUp" };
		static const char* const negativeBoxes[] = { "LeftThumbLeft", "LeftThumbDown", "RightThumbLeft", "RightThumbDown" };
		static const char* const triggerBoxes[] = { "LeftTrigger", "RightTrigger" };
		std::vector<std::pair<std::string, std::string>> boxes;
		auto add = [&boxes](const char* box, const std::string& value)
		{
			if (!value.empty())
				boxes.push_back(std::make_pair(std::string(box), value));
		};
		for (u32 i = 0; i < _countof(mapping.Button); ++i)
			add(buttonBoxes[i], Text(mapping.Button[i].type, mapping.Button[i].id));
		if (mapping.guide >= 0)
			add("ButtonGuide", std::to_string(mapping.guide + 1));
		if (mapping.DpadPOV > 0)
			add("DPad", "p" + std::to_string(mapping.DpadPOV));
		for (u32 i = 0; i < _countof(mapping.Trigger); ++i)
			add(triggerBoxes[i], Text(mapping.Trigger[i].type, mapping.Trigger[i].id));
		for (u32 i = 0; i < _countof(mapping.Axis); ++i)
		{
			const Config::AxisMap& a = mapping.Axis[i];
			if (a.analogType == Config::NONE && (IsHalf(a.positiveType) || IsHalf(a.negativeType)))
			{
				// The stick is its positive direction less its negative, as x360ce 4's formula writes it.
				std::string formula = "=" + (a.positiveButtonID ? Term(a.positiveType, a.positiveButtonID) : std::string());
				if (a.negativeButtonID)
					formula += "-" + Term(a.negativeType, a.negativeButtonID);
				add(axisBoxes[i], formula);
				continue;
			}
			add(axisBoxes[i], Text(a.analogType, a.id));
			add(positiveBoxes[i], Text(a.positiveType, a.positiveButtonID));
			add(negativeBoxes[i], Text(a.negativeType, a.negativeButtonID));
		}
		std::sort(boxes.begin(), boxes.end());
		std::string text;
		for (const auto& box : boxes)
		{
			if (!text.empty())
				text += " ";
			text += box.first + "=" + box.second;
		}
		return text;
	}
}

/// <summary>Test hook: the mapping an SDL row gives a device of the given controls, as Sdl::Describe writes it.</summary>
/// <remarks>Tests\Common\SdlNativeTest.cs runs the cases x360ce 4's converter passes through it. Returns the length the text needs, its terminating nought included.</remarks>
extern "C" DWORD WINAPI SdlTranslate(LPCSTR row, DWORD axisMask, DWORD sliderMask, DWORD buttonCount, DWORD povCount, LPSTR buffer, DWORD size)
{
	Sdl::Device device;
	device.axisMask = axisMask;
	device.sliderMask = sliderMask;
	device.buttonCount = buttonCount;
	device.povCount = povCount;
	Config::Mapping mapping;
	Sdl::Convert(row ? row : "", device, &mapping);
	std::string text = Sdl::Describe(mapping);
	if (buffer && size > 0)
		strncpy_s(buffer, size, text.c_str(), _TRUNCATE);
	return (DWORD)text.size() + 1;
}

/// <summary>The row a database finds for a vendor, product and name, copied for a test hook.</summary>
static DWORD FindForTest(bool x360ce, DWORD vendorId, DWORD productId, LPCSTR name, LPSTR buffer, DWORD size)
{
	Sdl::Device device;
	device.vendorId = (u16)vendorId;
	device.productId = (u16)productId;
	device.name = name ? name : "";
	std::string text = x360ce ? Sdl::FindX360ceRow(device) : Sdl::FindRow(device);
	if (buffer && size > 0)
		strncpy_s(buffer, size, text.c_str(), _TRUNCATE);
	return (DWORD)text.size() + 1;
}

/// <summary>Test hook: the SDL row the library finds for a vendor, product and name, empty for none. Returns the length the text needs, its terminating nought included.</summary>
extern "C" DWORD WINAPI SdlFind(DWORD vendorId, DWORD productId, LPCSTR name, LPSTR buffer, DWORD size)
{
	return FindForTest(false, vendorId, productId, name, buffer, size);
}

/// <summary>Test hook: the x360ce database row the library finds for a vendor, product and name, empty for none.</summary>
extern "C" DWORD WINAPI X360ceFind(DWORD vendorId, DWORD productId, LPCSTR name, LPSTR buffer, DWORD size)
{
	return FindForTest(true, vendorId, productId, name, buffer, size);
}
