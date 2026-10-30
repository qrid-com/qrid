# Qrs.App.SshAskPass.Cli (`qrs-ssh-askpass`)

An [`SSH_ASKPASS`](https://man.openbsd.org/ssh-add) helper, built as an Avalonia UI
application, that releases an ssh key passphrase only after an explicit confirmation
in the QRS Mobile App.

The passphrase is never stored in plaintext: it lives inside a **qrs-encrypted**
envelope in a well known per-user location and is decrypted on demand through the
standard QRS wrap/unwrap flow (the QRS Mobile App holds the Master Key / KEK).

Works on **Linux, macOS and Windows** (the UI is Avalonia).

## How `SSH_ASKPASS` works

When `ssh` / `ssh-add` need a passphrase but have no controlling terminal (or
`SSH_ASKPASS_REQUIRE=force` is set), they launch the program named by the
`SSH_ASKPASS` environment variable with a **single argument: the prompt string**,
and read the passphrase from the program's **stdout**.

Typical prompts (the wording varies between OpenSSH versions and tools):

| Tool      | Prompt example                                              |
|-----------|------------------------------------------------------------|
| `ssh-add` | `Enter passphrase for /home/alice/.ssh/id_rsa: `           |
| `ssh-add` | `Enter passphrase for /home/alice/.ssh/id_rsa (will confirm each use): ` |
| `ssh`     | `Enter passphrase for key '/home/alice/.ssh/id_ed25519': ` |

`qrs-ssh-askpass` parses this prompt to extract the **ssh key file name**. The host
name and the ssh key file name are then used as two separate Associated Data items in
the QRS flow, so the QRS Mobile App can show exactly *which key on which host* a
passphrase is being released for before you confirm.

## Storage location

By default the encrypted passphrase and the QRS client state are stored under the
per-user configuration directory:

* Linux / macOS: `~/.config/qrs-ssh-askpass/prod/ssh-password.qrs`
* Windows: `%APPDATA%\qrs-ssh-askpass\prod\ssh-password.qrs`

Use `-f|--file <FILE>` to override the location (e.g. one file per key).

## Usage

### 1. Initialise (store the passphrase)

```bash
qrs-ssh-askpass init [<ssh-key-file>] [--file <FILE>] [--force]
```

Enter the ssh key file name and its passphrase in the window, then scan the QR code
with your QRS Mobile App to qrs-encrypt and store it.

### 2. Use it as the askpass helper

```bash
export SSH_ASKPASS=/path/to/qrs-ssh-askpass
export SSH_ASKPASS_REQUIRE=force   # OpenSSH 8.4+: always use the askpass program
ssh-add ~/.ssh/id_rsa
```

`qrs-ssh-askpass` shows a QR code, you confirm the operation in the QRS Mobile App,
and the decrypted passphrase is written to stdout for `ssh` to consume. If anything
fails or is cancelled, the program exits non-zero and prints nothing to stdout.
