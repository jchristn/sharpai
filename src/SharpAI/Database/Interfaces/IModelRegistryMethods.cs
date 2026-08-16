namespace SharpAI.Database.Interfaces
{
    using System;
    using System.Collections.Generic;

    using SharpAI.Models;

    /// <summary>
    /// Domain-specific data-access methods for the model registry. Implementations use handwritten,
    /// provider-aware SQL rather than a generic ORM.
    /// </summary>
    public interface IModelRegistryMethods
    {
        /// <summary>
        /// Page through model file records using an enumeration query. This is the only list operation;
        /// there is no unbounded "get all".
        /// </summary>
        /// <param name="query">Enumeration query (paging, ordering, filters). When null, defaults are used.</param>
        /// <returns>Enumeration result.</returns>
        EnumerationResult<ModelFile> Enumerate(EnumerationQuery query);

        /// <summary>
        /// Retrieve a model file by GUID.
        /// </summary>
        /// <param name="guid">Model GUID.</param>
        /// <returns>The model file, or null when not found.</returns>
        ModelFile GetByGuid(Guid guid);

        /// <summary>
        /// Retrieve a model file by name.
        /// </summary>
        /// <param name="name">Model name.</param>
        /// <returns>The model file, or null when not found.</returns>
        ModelFile GetByName(string name);

        /// <summary>
        /// Retrieve multiple model files by GUID.
        /// </summary>
        /// <param name="guids">Model GUIDs.</param>
        /// <returns>Matching model files.</returns>
        List<ModelFile> GetMany(List<Guid> guids);

        /// <summary>
        /// Determine whether a model file with the given GUID exists.
        /// </summary>
        /// <param name="guid">Model GUID.</param>
        /// <returns>True if a record exists.</returns>
        bool ExistsByGuid(Guid guid);

        /// <summary>
        /// Insert a model file. If a record with the same name already exists, the existing record is
        /// returned unchanged.
        /// </summary>
        /// <param name="modelFile">Model file to insert.</param>
        /// <returns>The stored model file.</returns>
        ModelFile Add(ModelFile modelFile);

        /// <summary>
        /// Update an existing model file (matched by GUID).
        /// </summary>
        /// <param name="modelFile">Model file to update.</param>
        /// <returns>The updated model file.</returns>
        ModelFile Update(ModelFile modelFile);

        /// <summary>
        /// Delete a model file by GUID.
        /// </summary>
        /// <param name="guid">Model GUID.</param>
        void Delete(Guid guid);
    }
}
