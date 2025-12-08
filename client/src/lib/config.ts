const PROD = "https://jerneif25b.fly.dev";
const DEV  = "http://localhost:5284"; 

export const API_BASE = (import.meta.env.PROD ? PROD : DEV).replace(/\/+$/, "");
