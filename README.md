# 🏆 Hackathon Management Platform

> Complete platform for organizing programming competitions with automated scoring

## 🚀 Quick Overview

**Hackathon Management Platform** is a full-stack solution that enables organizations to run data science competitions with automated evaluation, real-time leaderboards, and comprehensive admin tools.

## 💻 Tech Stack

### Backend
- **ASP.NET Core 9.0** - High-performance REST API
- **Supabase** - PostgreSQL + Auth + Storage
- **Docker** - Containerized deployment

### Frontend  
- **Flutter Web** - Cross-platform UI

### Infrastructure
- **Docker Compose** - Easy deployment

## 🎯 Core Features

### ✅ Automated Scoring System
- **5 evaluation metrics**: Accuracy, F1 Score, MSE, MAE, RMSE
- **Background processing** - submissions evaluated asynchronously
- **Duplicate detection** using SHA256 file hashing
- **Suspicious score flagging** for fraud prevention

### ✅ Real-time Leaderboard
- Live ranking with best scores
- Username display instead of UUIDs
- Configurable top-N results
- Automatic refresh in Flutter UI

### ✅ Complete Submission Pipeline
- File upload with validation (CSV, JSON, Python)
- Rate limiting (5 submissions/hour per user)
- Automatic background evaluation
- Status tracking: pending → processing → completed

### ✅ Admin Management Console
- Full CRUD for challenges
- Ground-truth file management
- Manual scoring capabilities
- User role management

### ✅ Security & Fair Play
- JWT authentication via Supabase
- Rate limiting on all endpoints
- File size and type validation
- Row Level Security (RLS) policies


