#!/usr/bin/env python3
"""IntelliLink CLI tool for plugin installation, user management, and health checks."""

import argparse
import json
import os
from typing import List

# Data storage paths relative to this script
DATA_DIR = os.path.dirname(os.path.abspath(__file__))
USER_FILE = os.path.join(DATA_DIR, "users.json")
PLUGIN_FILE = os.path.join(DATA_DIR, "plugins.json")

def _load_list(path: str) -> List[str]:
    """Load a list from a JSON file."""
    if os.path.exists(path):
        try:
            with open(path, "r", encoding="utf-8") as handle:
                data = json.load(handle)
                if isinstance(data, list):
                    return data
        except json.JSONDecodeError:
            pass
    return []

def _save_list(path: str, data: List[str]) -> None:
    """Persist a list to a JSON file."""
    with open(path, "w", encoding="utf-8") as handle:
        json.dump(data, handle, indent=2)

def install_plugin(name: str) -> None:
    """Install a plugin by name."""
    plugins = _load_list(PLUGIN_FILE)
    if name in plugins:
        print(f"Plugin '{name}' already installed.")
    else:
        plugins.append(name)
        _save_list(PLUGIN_FILE, plugins)
        print(f"Plugin '{name}' installed.")

def user_add(name: str) -> None:
    """Add a new user."""
    users = _load_list(USER_FILE)
    if name in users:
        print(f"User '{name}' already exists.")
    else:
        users.append(name)
        _save_list(USER_FILE, users)
        print(f"User '{name}' added.")

def user_remove(name: str) -> None:
    """Remove a user."""
    users = _load_list(USER_FILE)
    if name in users:
        users.remove(name)
        _save_list(USER_FILE, users)
        print(f"User '{name}' removed.")
    else:
        print(f"User '{name}' does not exist.")

def user_list() -> None:
    """List all users."""
    users = _load_list(USER_FILE)
    if users:
        for user in users:
            print(user)
    else:
        print("No users found.")

def health_check() -> None:
    """Perform a simple health check."""
    print("OK")

def build_parser() -> argparse.ArgumentParser:
    """Construct the argument parser for the CLI."""
    parser = argparse.ArgumentParser(description="IntelliLink command line tool")
    subparsers = parser.add_subparsers(dest="command")

    # Plugin commands
    plugin_parser = subparsers.add_parser("plugin", help="Plugin operations")
    plugin_sub = plugin_parser.add_subparsers(dest="plugin_command")
    install_parser = plugin_sub.add_parser("install", help="Install a plugin")
    install_parser.add_argument("name", help="Plugin name")

    # User commands
    user_parser = subparsers.add_parser("user", help="User management")
    user_sub = user_parser.add_subparsers(dest="user_command")
    user_add_parser = user_sub.add_parser("add", help="Add a user")
    user_add_parser.add_argument("name")
    user_remove_parser = user_sub.add_parser("remove", help="Remove a user")
    user_remove_parser.add_argument("name")
    user_sub.add_parser("list", help="List users")

    # Health check
    subparsers.add_parser("health", help="Perform health check")

    return parser

def main(argv: List[str] | None = None) -> None:
    parser = build_parser()
    args = parser.parse_args(argv)

    if args.command == "plugin":
        if args.plugin_command == "install":
            install_plugin(args.name)
        else:
            plugin_parser = build_parser()._subparsers._actions[1].choices["plugin"]
            plugin_parser.print_help()
    elif args.command == "user":
        if args.user_command == "add":
            user_add(args.name)
        elif args.user_command == "remove":
            user_remove(args.name)
        elif args.user_command == "list":
            user_list()
        else:
            user_parser = build_parser()._subparsers._actions[1].choices["user"]
            user_parser.print_help()
    elif args.command == "health":
        health_check()
    else:
        parser.print_help()

if __name__ == "__main__":
    main()
