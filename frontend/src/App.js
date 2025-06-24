import React, { useEffect, useState } from "react";

function App() {
  const [mangas, setMangas] = useState([]);

  useEffect(() => {
    fetch("https://mangasrepo-production.up.railway.app/api/manga", {
      method: "GET",
      headers: {
        "Content-Type": "application/json",
        "Authorization": "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJhY2Nlc3MiOiJhcGkiLCJleHAiOjE3NTA3OTE4MzB9.ptwmVye0qg3HCyzgL0IGTknbysRSHIjKklJ0UNnKqkA"
      }
    })
      .then((response) => {
        if (!response.ok) {
          throw new Error("Error al obtener mangas");
        }
        return response.json();
      })
      .then((data) => {
        setMangas(data);
      })
      .catch((error) => {
        console.error("Error al conectar con la API:", error);
      });
  }, []);

  return (
    <div style={{ padding: "20px" }}>
      <h1>Lista de Mangas</h1>
      <ul>
        {mangas.map((manga) => (
          <li key={manga.id}>
            <strong>{manga.titulo}</strong> - {manga.autor}
          </li>
        ))}
      </ul>
    </div>
  );
}

export default App;

