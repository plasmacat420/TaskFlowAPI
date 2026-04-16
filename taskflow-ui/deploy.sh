#!/usr/bin/env bash
# deploy.sh — GitHub Pages Deployment Script for TaskFlow UI
# Repo: plasmacat420/TaskFlowAPI → https://plasmacat420.github.io/TaskFlowAPI/
set -e

REPO_NAME="TaskFlowAPI"

echo "Building TaskFlow UI for GitHub Pages (base-href: /${REPO_NAME}/)..."

npx ng build \
  --configuration production \
  --base-href "/${REPO_NAME}/"

# ── 404.html fix for Angular SPA on GitHub Pages ──────────────────────────
# GitHub Pages returns 404 for deep links like /TaskFlowAPI/tasks because
# there is no tasks/index.html file. Copying index.html → 404.html means
# GitHub Pages serves the Angular app for any unknown URL, then Angular
# Router reads the URL and renders the correct component client-side.
echo "Copying index.html → 404.html for SPA deep-link support..."
cp "dist/${REPO_NAME}/browser/index.html" "dist/${REPO_NAME}/browser/404.html"

echo "Deploying to gh-pages branch..."
npx angular-cli-ghpages \
  --dir "dist/${REPO_NAME}/browser"

echo ""
echo "Deployed! Live at: https://plasmacat420.github.io/${REPO_NAME}/"
echo "(GitHub Pages may take 1-2 minutes to update)"
