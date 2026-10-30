#!/bin/bash

echo "aws_cli.sh is running:"
echo "These 2 environment variables have been DECRYPTED from your YML file and set available for this process only:"
echo "----------------------------------------"
echo "AWS_ACCESS_KEY_ID=$AWS_ACCESS_KEY_ID"
echo "AWS_SECRET_ACCESS_KEY=$AWS_SECRET_ACCESS_KEY"
echo "----------------------------------------"
echo "aws ecr list-images --region us-west-2 --repository-name qridproxyserver"

aws ecr list-images --region us-west-2 --repository-name qridproxyserver
