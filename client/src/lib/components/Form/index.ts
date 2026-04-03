import Form from "./Form.svelte";
import FieldError from "./FieldError.svelte";
import { createFormCreator, createFormCreatorContexts } from "@tanstack/svelte-form";
import Input from "./Input.svelte";
import Submit from "./Submit.svelte";

const isInvalid = (field: any) => {
	return field.state.meta.isTouched && !field.state.meta.isValid;
};

const { createAppForm } = createFormCreator({
	fieldComponents: {
		Input,
	},
	formComponents: {
		Submit,
	},
});

const { useFieldContext, useFormContext } = createFormCreatorContexts();

export { Form, FieldError, isInvalid, createAppForm, useFieldContext, useFormContext };
