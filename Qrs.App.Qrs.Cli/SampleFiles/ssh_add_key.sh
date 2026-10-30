#!/bin/bash

ssh-add -t 120 - <<< "$SSH_PRIVATE_KEY"
echo "Your SSH key has been decrypted and added to your ssh agent. Please start your SSH session now!"
