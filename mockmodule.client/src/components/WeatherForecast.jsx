import { useEffect, useState } from 'react';
import { getWeatherForecasts } from '../api/weatherApi';

function WeatherForecast() {
    const [forecasts, setForecasts] = useState();
    const [error, setError] = useState(false);

    useEffect(() => {
        populateWeatherData();
    }, []);

    async function populateWeatherData() {
        const data = await getWeatherForecasts();
        if (data) {
            setForecasts(data);
            setError(false);

        } else {
            setError(true);
        }
    }

    const contents = error
        ? <p><em>Something went wrong loading forecast.</em></p>
        : forecasts === undefined
            ? <p><em>Loading... Please refresh once the ASP.NET backend has started. See <a href="https://aka.ms/jspsintegrationreact">https://aka.ms/jspsintegrationreact</a> for more details.</em></p>
            : <table className="table table-striped" aria-labelledby="tableLabel">
                <thead>
                    <tr>
                        <th>Date</th>
                        <th>Temp. (C)</th>
                        <th>Temp. (F)</th>
                        <th>Summary</th>
                    </tr>
                </thead>
                <tbody>
                    {forecasts.map(forecast =>
                        <tr key={forecast.date}>
                            <td>{forecast.date}</td>
                            <td>{forecast.temperatureC}</td>
                            <td>{forecast.temperatureF}</td>
                            <td>{forecast.summary}</td>
                        </tr>
                    )}
                </tbody>
            </table>;

    return (
        <div>
            <h1>Weather forecast</h1>
            <button onClick={populateWeatherData}>Refresh Data</button>
            <center>
                {contents}
            </center>
        </div>
    );
}

export default WeatherForecast;