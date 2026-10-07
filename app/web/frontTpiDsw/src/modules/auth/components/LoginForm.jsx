import Input from '../../shared/components/Input.jsx'
import { useForm } from 'react-hook-form'
import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import useAuth from '../hooks/useAuth.js';
import toast from 'react-hot-toast';

function LoginForm() {

    const [loginError, setLoginError] = useState('');

    const navigate = useNavigate();

    const {
      register,
      handleSubmit,
    } = useForm({mode: 'onSubmit'});

    const { signin } = useAuth();

    const onValid = async (formData) => {
        const { error, role } = await signin(formData.username, formData.password);

        if (error) {
            setLoginError(error);
            return;
        }
        toast.success('Inicio de sesión exitoso');
        if (role === 'Client') {
            navigate('/');
        } else {
            navigate('/admin/home');
        }
    };

  return (
    <form className='flex
        flex-col
        gap-20
        bg-white
        p-8
        sm:w-md
        sm:gap-4
        sm:rounded-lg
        sm:shadow-lg
      '
      onSubmit={handleSubmit(onValid)}>
        <Input 
           label='Usuario:' 
           {...register('username')}
           />

        <Input 
          label='Contraseña:'
          {...register('password')}
          type='password'
          />
        {loginError && <p className='text-red-500 text-sm'>{loginError}</p>}
        <button type="submit"> Iniciar Sesión </button>
        <button type="button" onClick={() => navigate('/register')}> Registrar usuario </button>
    </form>  
  );
}

export default LoginForm