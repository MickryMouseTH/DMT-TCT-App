# LSD_Bridge — ตัวแปลงคำสั่ง LSD (ระบบเดิม → ระบบใหม่)

แปลงคำสั่งสั่งงานป้าย LSD จากระบบเดิม (**TCT_App**, TCP binary โปรโตคอล `AVON`)
ให้กลายเป็นการเรียก **REST API** ของ LSD Controller ตัวใหม่

```
[ TCT_App เดิม ]  --TCP 16603 (AVON binary)-->  [ LSD_Bridge ]  --HTTP PUT-->  [ LSD Controller ใหม่ 169.254.32.1 ]
```

Bridge ทำตัว "เสมือนเป็น LSD ตัวจริง": เปิด TCP server ให้ TCT_App ต่อเข้ามา,
ถอด frame, แล้วเฉพาะคำสั่ง **Show/Clear ป้าย** จะถูกแปลงเป็น HTTP ไปยิงตัวใหม่

---

## ไฟล์ในโฟลเดอร์
| ไฟล์ | หน้าที่ |
|------|--------|
| `LSD_Bridge.py` | โปรแกรมหลัก (TCP server + HTTP client) |
| `avon_protocol.py` | ถอด/เข้ารหัส frame โปรโตคอล AVON เดิม + CRC |
| `LogLibrary.py` | ไลบรารี log/config (loguru) — ของเดิมที่ให้มา |
| `requirements.txt` | dependency |
| `LSD_Bridge_config.json` | ไฟล์ config (สร้างอัตโนมัติครั้งแรกที่รัน) |

## ติดตั้ง & รัน
```bash
cd LSD_Contoller
python3 -m pip install -r requirements.txt
python3 LSD_Bridge.py
```
รันครั้งแรกจะสร้าง `LSD_Bridge_config.json` ให้ — แก้ค่าแล้วรันใหม่

## การตั้งค่า (LSD_Bridge_config.json)
| key | ความหมาย | default |
|-----|----------|---------|
| `Listen_Host` / `Listen_Port` | TCP ที่ Bridge เปิดรับจาก TCT_App เดิม | `0.0.0.0` / `16603` |
| `LSD_New_BaseURL` | URL ตัว LSD controller ใหม่ | `http://169.254.32.1` |
| `LSD_New_ActivatePath` | path activate (`{vsn}` จะถูกแทน) | `/api/vsns/sources/lan/vsns/{vsn}/activated` |
| `LSD_New_Method` | HTTP method | `PUT` |
| `LSD_Auth_Header` / `LSD_Auth_Value` | header auth (จาก Postman) | `Authorization` / `YW5vbnltb3VzOg==` |
| `HTTP_Timeout` | timeout (วินาที) | `5` |
| `Ack_Enable` | ตอบ ACK กลับระบบเดิม (ควรเปิด) | `1` |
| `Forward_Clear` | ยิง deactivate ตอนได้คำสั่ง Clear | `0` |
| `Identifier_Map` | map identifier เดิม → ชื่อ `.vsn` | ดูด้านล่าง |

### การเชื่อมฝั่ง TCT_App เดิม
ในไฟล์ config ของ TCT_App ให้ชี้ LSD มาที่เครื่องที่รัน Bridge:
```
LSD.Enable     = 1
LSD.IPAddress  = <IP ของเครื่องที่รัน LSD_Bridge>   (ถ้าเครื่องเดียวกันใช้ 127.0.0.1)
LSD.SocketPort = 16603
```

## การ map คำสั่ง (หัวใจของ Converter)

คำสั่งเดิมทุกโหมด **Show ใช้ opcode เดียวกัน** ต่างกันแค่ค่า `identifier` ใน payload
Bridge จึง map ตาม `identifier`:

| โหมดเดิม (LsdMode) | identifier เดิม (default) | → ชื่อ `.vsn` ระบบใหม่ |
|--------------------|---------------------------|------------------------|
| LSD_CLOSE (ปิดช่อง) | `6bed8fbc88534dc275692569e153633e` | `CLOSE.vsn` |
| LSD_MTC            | `993bd1ac3e9c645360110be0aa4a0f83` | `MTC.vsn` |
| LSD_ETC            | `f5e7e2d08013ceed5752de941f22c266` | `ETC.vsn` |
| LSD_DUAL (ETC-MTC) | `80b019ebaea588cbd8135b8753b48922` | `ETC-MTC.vsn` |

> **หมายเหตุ:** identifier ของ LSD **แต่ละเครื่องไม่เหมือนกัน** — เครื่องจริงแจกรายการ
> name/identifier ของตัวเองผ่าน response ของ SYNC2 (frame ยาว ~1.5KB)
> - 4 ค่าในตารางคือค่า hardcode ในระบบเดิม (fallback เมื่อไม่ได้ response SYNC2 ยาว)
> - Bridge ตอบ ACK สั้น (< 60 ไบต์) ให้ SYNC2 เสมอ → TCT_App คง identifier default ไว้ → map ตรง
> - ค่า default ใน `Identifier_Map` จึงใส่ทั้งชุด hardcode เดิม **และ** ชุดของเครื่อง LSD จริง
>   (เครื่อง `172.30.182.98`: `CLOSE`/`MTC`/`ETC`/`ETC-MTC`/`MTC-N`/`ME-N`) — หลาย identifier
>   ชี้ `.vsn` เดียวกันได้
> - ถ้าหน้างานใช้ identifier อื่น: กดปุ่ม `SYNC2` ใน NovaDemo กับเครื่องนั้น มันจะ log
>   identifier จริงและอัปเดต `Identifier_Map` ของตัวเองอัตโนมัติ แล้วนำค่ามาเพิ่มใน config นี้

## คำสั่งที่ Bridge จัดการ
| คำสั่งเดิม | การกระทำ |
|-----------|----------|
| `KeepAlive`, `LORA_SYNC`, `ROTATION`, `SYNC1/2/3` | ตอบ ACK เฉย ๆ (ไม่ยิง HTTP) — แค่หล่อเลี้ยง init/keepalive ของระบบเดิม |
| `Show*` (LaneClose/MTC/ETC/Dual) | map identifier → `PUT .../{vsn}/activated` |
| `Clear*` | log (ยิง deactivate ถ้า `Forward_Clear=1`) |

## ทดสอบโปรโตคอล (ไม่ต้องมีของจริง)
```bash
python3 test_protocol.py
```
