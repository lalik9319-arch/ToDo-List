import axios from 'axios';

// הגדרת כתובת API כ-default
axios.defaults.baseURL = "http://localhost:5095";

// interceptor שמדפיס שגיאות
axios.interceptors.response.use(
  response => response,
  error => {
    console.error("API Error:", error);
    return Promise.reject(error);
  }
);

export default {
  getTasks: async () => {
    const result = await axios.get("/tasks"); // עכשיו הבסיס כבר מוגדר
    return result.data;
  },

  addTask: async(name) => {
    const result = await axios.post("/tasks", { name, isComplete: false });
    return result.data;
  },

  setCompleted: async(id, isComplete) => {
    await axios.put(`/tasks/${id}`, { isComplete });
  },

  deleteTask: async(id) => {
    await axios.delete(`/tasks/${id}`);
  }
};
