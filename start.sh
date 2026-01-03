#!/bin/bash

# Colors for output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

echo -e "${BLUE}🚀 Starting Eloomen Development Environment...${NC}\n"

# Function to cleanup on exit
cleanup() {
    echo -e "\n${YELLOW}🛑 Shutting down servers...${NC}"
    kill $SERVER_PID $CLIENT_PID 2>/dev/null
    exit
}

# Trap Ctrl+C and call cleanup
trap cleanup INT TERM

# Check if .NET is installed
if ! command -v dotnet &> /dev/null; then
    echo -e "${RED}❌ .NET SDK not found. Please install .NET SDK.${NC}"
    exit 1
fi

# Check if Node.js is installed
if ! command -v node &> /dev/null; then
    echo -e "${RED}❌ Node.js not found. Please install Node.js.${NC}"
    exit 1
fi

# Check if npm is installed
if ! command -v npm &> /dev/null; then
    echo -e "${RED}❌ npm not found. Please install npm.${NC}"
    exit 1
fi

# Start .NET server with watch (auto-reload on changes)
echo -e "${GREEN}📦 Starting .NET Server (port 3000) with hot reload...${NC}"
cd server
dotnet watch run &
SERVER_PID=$!
cd ..

# Wait a moment for server to start
sleep 2

# Start Next.js client (auto-reload on changes)
echo -e "${GREEN}📦 Starting Next.js Client (port 3001)...${NC}"
cd client
npm run dev &
CLIENT_PID=$!
cd ..

echo -e "\n${BLUE}✅ Both servers are starting...${NC}"
echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${GREEN}🌐 Server:${NC} http://localhost:3000"
echo -e "${GREEN}🌐 Client:${NC} http://localhost:3001"
echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${YELLOW}💡 Press Ctrl+C to stop both servers${NC}\n"

# Wait for both processes
wait

