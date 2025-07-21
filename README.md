# Adesso World League - Draw System  

### 🚀 **Project Overview**  
A .NET Core API that randomly distributes **32 teams** from **8 countries** into **4 or 8 groups** for a tournament, ensuring fair distribution.  

### ✅ **Key Features**  
✔ **Automated Draw** – Randomly assigns teams to groups  
✔ **Fair Rules** – No two teams from the same country in a group  
✔ **Persistent Data** – Saves draw results with the drawer's name  
✔ **Validation** – Only allows **4 or 8 groups**  

### ⚙ **Tech Stack**  
- **Backend**: .NET Core  
- **Database**: EF Core, Redis (cache), Elasticsearch (search)  
- **Messaging**: RabbitMQ (events)  
- **Testing**: Unit Tests  
- **Deployment**: Docker  

### 📌 **How It Works**  
1. **Input**:  
   - `drawerName` (who performed the draw)  
   - `groupCount` (must be **4** or **8**)  

2. **Output**:  
   ```json
   {
     "groups": [
       {
         "groupName": "A",
         "teams": ["Adesso İstanbul", "Adesso Berlin", ...]
       },
       ...
     ]
   }
   ```

### 🚀 **Quick Start**  
1. Run with Docker:  
   ```bash
   docker-compose up -d
   dotnet run
   ```
2. Call the API:  
   ```bash
   POST /api/draws  
   { "drawerName": "Your Name", "groupCount": 8 }
   ```

### 📄 **Rules**  
- **4 groups** = 8 teams per group (all countries represented)  
- **8 groups** = 4 teams per group (half the countries per group)  
- Teams are assigned in a **round-robin** order  

🔗 **API Docs**: `GET /swagger` for full details  

---  
**🏆 Happy Drawing!** 🏆
