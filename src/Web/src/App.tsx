import TransferForm from './components/TransferForm';
import './App.css';

function App() {
  return (
    <>
      <header>
        <h1>KSP Navigation Computer</h1>
        <p>Interplanetary transfer planner — stock bodies</p>
      </header>
      <main>
        <TransferForm />
      </main>
    </>
  );
}

export default App;