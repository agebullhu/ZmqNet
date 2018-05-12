#pragma once
#include "stdinc.h"

#include "NetCommand/net_command.h"
#include "tson/tson_def.h"
#include "tson/tson_deserializer.h"
#include "tson/tson_serializer.h"

#include <cfg/config.h>
#include "redis/redis.h"
#include "debug/TraceStack.h"

#define WIN32_LEAN_AND_MEAN             // �� Windows ͷ���ų�����ʹ�õ�����
// Windows ͷ�ļ�: 
#include <windows.h>
#define _ATL_CSTRING_EXPLICIT_CONSTRUCTORS      // ĳЩ CString ���캯��������ʽ��
#ifdef WINDOWS_SERVICE
#include <atlbase.h>
#include <atlcom.h>
#include <atlctl.h>
#endif