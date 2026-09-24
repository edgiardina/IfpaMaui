r"""Print the IFPA Companion App Store versions and the latest uploaded builds.

Read-only. Uses the App Store Connect API key in C:\Users\ed_gi\.pinventory (the same key as the
ASC_* repository secrets). The issuer file is a hex DPAPI blob with a UTF-16LE plaintext.
Needs the cryptography and requests packages (Windows only, because of DPAPI).
"""
import base64
import ctypes
import ctypes.wintypes as wt
import json
import time

import requests
from cryptography.hazmat.primitives import hashes, serialization
from cryptography.hazmat.primitives.asymmetric import ec
from cryptography.hazmat.primitives.asymmetric.utils import decode_dss_signature

KEY_DIR = r"C:\Users\ed_gi\.pinventory"
KEY_ID = "G2QKQC888F"
BUNDLE_ID = "com.edgiardina.ifpa"


class _Blob(ctypes.Structure):
    _fields_ = [("cbData", wt.DWORD), ("pbData", ctypes.POINTER(ctypes.c_char))]


def _dpapi(hex_text):
    raw = bytes.fromhex(hex_text.strip())
    buf = ctypes.create_string_buffer(raw, len(raw))
    blob_in = _Blob(len(raw), ctypes.cast(buf, ctypes.POINTER(ctypes.c_char)))
    blob_out = _Blob()
    if not ctypes.windll.crypt32.CryptUnprotectData(ctypes.byref(blob_in), None, None, None, None, 0, ctypes.byref(blob_out)):
        raise OSError("CryptUnprotectData failed")
    data = ctypes.string_at(blob_out.pbData, blob_out.cbData)
    ctypes.windll.kernel32.LocalFree(blob_out.pbData)
    return data.decode("utf-16-le")


def _b64(data):
    return base64.urlsafe_b64encode(data).rstrip(b"=").decode()


def _token():
    issuer = _dpapi(open(rf"{KEY_DIR}\asc-issuer.key").read())
    key = serialization.load_pem_private_key(open(rf"{KEY_DIR}\AuthKey_{KEY_ID}.p8", "rb").read(), None)
    now = int(time.time())
    header = _b64(json.dumps({"alg": "ES256", "kid": KEY_ID, "typ": "JWT"}).encode())
    payload = _b64(json.dumps({"iss": issuer, "iat": now, "exp": now + 1100, "aud": "appstoreconnect-v1"}).encode())
    r, s = decode_dss_signature(key.sign(f"{header}.{payload}".encode(), ec.ECDSA(hashes.SHA256())))
    return f"{header}.{payload}.{_b64(r.to_bytes(32, 'big') + s.to_bytes(32, 'big'))}"


def _get(path, **params):
    response = requests.get(
        "https://api.appstoreconnect.apple.com" + path,
        headers={"Authorization": "Bearer " + _token()},
        params=params,
        timeout=30,
    )
    response.raise_for_status()
    return response.json()


def main():
    app = _get("/v1/apps", **{"filter[bundleId]": BUNDLE_ID})["data"][0]
    print(f"app {app['attributes']['name']} ({app['id']})")
    for version in _get(f"/v1/apps/{app['id']}/appStoreVersions", limit=5)["data"]:
        a = version["attributes"]
        print(f"version {a['versionString']:<8} {a['appStoreState']}")
    builds = _get("/v1/builds", **{"filter[app]": app["id"], "sort": "-uploadedDate", "limit": 5, "include": "preReleaseVersion"})
    marketing = {i["id"]: i["attributes"]["version"] for i in builds.get("included", []) if i["type"] == "preReleaseVersions"}
    for build in builds["data"]:
        a = build["attributes"]
        pre = marketing.get(build["relationships"]["preReleaseVersion"]["data"]["id"], "?")
        print(f"build   {pre:<8} ({a['version']}) {a['processingState']} {a['uploadedDate'][:16]}")


if __name__ == "__main__":
    main()
