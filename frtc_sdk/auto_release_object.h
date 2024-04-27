#ifndef AUTO_RELEASE_OBJECT_H_
#define AUTO_RELEASE_OBJECT_H_

class AutoReleaseLock {
public:
    AutoReleaseLock(HANDLE mutex)
        : _mutex(mutex) {
        WaitForSingleObject(mutex, INFINITE);
    }

    ~AutoReleaseLock() {
        ReleaseMutex(_mutex);
    }

private:
    HANDLE _mutex;
};

class AutoReleaseSelector {
public:
    AutoReleaseSelector(HDC hdc, HGDIOBJ object)
        : _hdc(hdc), 
          _old_obj(NULL) {
        _old_obj = SelectObject(_hdc, object);
    }

    ~AutoReleaseSelector() {
        if (_old_obj != NULL) {
            SelectObject(_hdc, _old_obj);
        }
    }

    BOOL Successed() {
        return _old_obj != NULL;
    }

private:
    HGDIOBJ _old_obj;
    HDC _hdc;
};

template <typename T, typename R>
class AutoReleaseObject {
public:
    AutoReleaseObject(T obj, R func)
        : _data(obj), 
          _release_func(func){}

    ~AutoReleaseObject() {
        if (_data != NULL) {
            _release_func(_data);
        }
    }

    void Attach(T data) {
        _release_func(_data);
        _data = data;
    }

    T Detach() {
        T temp = _data;
        _data = NULL;
        return temp;
    }

    operator T() {
        return _data;
    }

private:
    T _data;
    R _release_func;
};

typedef BOOL(__stdcall *ReleaseHDCFunc)(HDC);
class AutoReleaseHDC : public AutoReleaseObject<HDC, ReleaseHDCFunc> {
public:
    AutoReleaseHDC(HDC hdc) : AutoReleaseObject(hdc, DeleteDC) {}
};

typedef BOOL(__stdcall *ReleaseHGDIFunc)(HGDIOBJ);
class AutoReleaseHGDI : public AutoReleaseObject<HGDIOBJ, ReleaseHGDIFunc> {
public:
    AutoReleaseHGDI(HGDIOBJ obj) : AutoReleaseObject(obj, DeleteObject) {}
};

typedef BOOL(__stdcall *ReleaseHCURSORFunc)(HCURSOR);
class AutoReleaseHCURSOR : public AutoReleaseObject<HCURSOR, ReleaseHCURSORFunc> {
public:
    AutoReleaseHCURSOR(HCURSOR obj) : AutoReleaseObject(obj, DestroyCursor) {}
};

typedef BOOL(__stdcall *ReleaseHICONEFunc)(HICON);
class AutoReleaseHICON : public AutoReleaseObject<HICON, ReleaseHICONEFunc> {
public:
    AutoReleaseHICON(HICON obj) : AutoReleaseObject(obj, DestroyIcon) {}
};

class ReleaseWinDCFunc {
public:
    ReleaseWinDCFunc(HWND wind)
        : _wind(wind) {
    }

    void operator()(HDC hdc) {
        ReleaseDC(_wind, hdc);
    }

private:
    HWND _wind;
};

class AutoReleaseWinDC : public AutoReleaseObject<HDC, ReleaseWinDCFunc> {
public:
    AutoReleaseWinDC(HDC hdc, HWND wnd)
        : AutoReleaseObject(hdc, ReleaseWinDCFunc(wnd)) {}
};

#endif
