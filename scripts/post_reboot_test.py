"""
Verify update server is running after reboot or deployment.
Usage: python post_reboot_test.py
Requires environment variables: UPDATE_SERVER_HOST, UPDATE_SERVER_USER,
  UPDATE_SERVER_PASSWORD, UPDATE_SERVER_SSH_PORT, UPDATE_SERVER_PORT
"""
import paramiko
import os


def ssh_exec(client, cmd):
    stdin, stdout, stderr = client.exec_command(cmd, timeout=30)
    exit_code = stdout.channel.recv_exit_status()
    out = stdout.read().decode("gbk", errors="replace")
    err = stderr.read().decode("gbk", errors="replace")
    return exit_code, out, err


def main():
    host = os.environ["UPDATE_SERVER_HOST"]
    user = os.environ["UPDATE_SERVER_USER"]
    passwd = os.environ["UPDATE_SERVER_PASSWORD"]
    ssh_port = int(os.environ.get("UPDATE_SERVER_SSH_PORT", "22"))
    http_port = int(os.environ.get("UPDATE_SERVER_PORT", "80"))

    client = paramiko.SSHClient()
    client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
    client.connect(host, port=ssh_port, username=user, password=passwd, timeout=15)

    nssm = r'"C:\WorkSpace\update-server\nssm.exe"'

    # 1. Service status
    print("1. Service status:")
    code, out, err = ssh_exec(client, f"{nssm} status DocuFillerUpdateServer")
    print(f"   {out.strip().replace(chr(0), '')}")

    # 2. Port listening
    print(f"2. Port {http_port}:")
    code, out, err = ssh_exec(client, f"netstat -ano | findstr :{http_port} | findstr LISTENING")
    print(f"   {'listening' if out.strip() else 'NOT listening'}")

    # 3. API tests
    print("\n3. API tests:")
    tests = [
        (f"GET /stable/releases.win.json", f"http://localhost:{http_port}/stable/releases.win.json"),
        (f"GET /beta/releases.win.json", f"http://localhost:{http_port}/beta/releases.win.json"),
        (f"GET /api/channels/stable/releases", f"http://localhost:{http_port}/api/channels/stable/releases"),
        (f"GET /api/channels/beta/releases", f"http://localhost:{http_port}/api/channels/beta/releases"),
    ]
    for label, url in tests:
        code, out, err = ssh_exec(
            client,
            f'powershell -Command "try {{ $r = Invoke-WebRequest -Uri {url} -UseBasicParsing -TimeoutSec 5; '
            f'Write-Host STATUS:$($r.StatusCode) }} catch {{ Write-Host ERROR:$($_.Exception.Response.StatusCode.value__) }}"',
        )
        mark = "OK" if "STATUS:200" in out else "FAIL"
        print(f"   [{mark}] {label} -> {out.strip()}")

    client.close()


if __name__ == "__main__":
    main()
